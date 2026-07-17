using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.Shared.Auth;
using PensionVault.Shared.Middleware;
using PensionVault.Shared.HttpClients;
using Claims.Data;
using Claims.Data.Repositories;
using Claims.Data.Seed;
using Claims.Domain.Repositories;
using Claims.Services;
using Claims.Services.HttpClients;
using Claims.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Database (using Claims DB)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(connString));

// Register Local Repositories and Services
builder.Services.AddScoped<IUnitOfWork, GenericUnitOfWork<ClaimsDbContext>>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();

// Register Proxy HttpClients
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<MembersServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<ContributionsServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<NotificationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<AuditServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
}).AddStandardResilienceHandler();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Controllers and Audit Filter
builder.Services.AddScoped<ClaimsAuditFilter>();
builder.Services.AddControllers(opts =>
    {
        opts.Filters.AddService<ClaimsAuditFilter>();
    })
    .AddJsonOptions(opts => {
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PensionVault Claims Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PensionVault Claims Service v1"));
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await db.Database.MigrateAsync();
    await ClaimsDataSeeder.SeedAsync(db);
}

Log.Information("PensionVault Claims Service starting on port 5002");
app.Run();




