using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.Shared.Auth;
using PensionVault.Shared.Middleware;
using PensionVault.Shared.HttpClients;
using Contributions.Data;
using Contributions.Data.Repositories;
using Contributions.Data.Seed;
using Contributions.Domain.Repositories;
using Contributions.Services;
using Contributions.Services.HttpClients;
using Contributions.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Database (using Contributions DB)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ContributionsDbContext>(options =>
    options.UseSqlServer(connString));

// Register Local Repositories and Services
builder.Services.AddScoped<IUnitOfWork, GenericUnitOfWork<ContributionsDbContext>>();
builder.Services.AddScoped<IFundAccountRepository, FundAccountRepository>();
builder.Services.AddScoped<IContributionRepository, ContributionRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<IShortfallRequestRepository, ShortfallRequestRepository>();

builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Register Proxy HttpClients
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<MemberServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
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
builder.Services.AddScoped<ContributionsAuditFilter>();
builder.Services.AddControllers(opts =>
    {
        opts.Filters.AddService<ContributionsAuditFilter>();
    })
    .AddJsonOptions(opts => {
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PensionVault Contributions Service", Version = "v1" });
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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PensionVault Contributions Service v1"));
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContributionsDbContext>();
    await db.Database.MigrateAsync();
    await ContributionsDataSeeder.SeedAsync(db);
}

Log.Information("PensionVault Contributions Service starting on port 5004");
app.Run();




