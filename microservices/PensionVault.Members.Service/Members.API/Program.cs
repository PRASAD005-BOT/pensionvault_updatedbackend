using System;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.Shared.Auth;
using PensionVault.Shared.Middleware;
using Members.Data;
using Members.Data.Repositories;
using Members.Data.Seed;
using Members.Domain.Repositories;
using Members.Services.ProxyRepositories;
using Members.Services;
using Members.API.Filters;
using Members.Services.ProxyServices;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Database (using Members DB)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MembersDbContext>(options =>
    options.UseSqlServer(connString));

// Register Local Repositories
builder.Services.AddScoped<IUnitOfWork, GenericUnitOfWork<MembersDbContext>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IFundSchemeRepository, FundSchemeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register Proxy Repositories using HTTP Client
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IFundAccountRepository, HttpFundAccountRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<IContributionRepository, HttpContributionRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<ILedgerRepository, HttpLedgerRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<ContributionsServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<IClaimRepository, HttpClaimRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ClaimsUrl"] ?? "http://localhost:5002/");
}).AddStandardResilienceHandler();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<ISchemeService, SchemeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Controllers
builder.Services.AddScoped<AuditLogFilter>();
builder.Services.AddControllers(opts =>
{
    opts.Filters.AddService<AuditLogFilter>();
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PensionVault Members Service", Version = "v1" });
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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PensionVault Members Service v1"));
}

app.UseCors("AllowAll");

/* ================== FIXED STATIC FILES SERVING ROUTE START ================== */
app.UseStaticFiles(); // Serves default static files out of wwwroot if they exist

// Map physical directory access rules explicitly to expose saved images
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var profilesPath = Path.Combine(webRootPath, "uploads", "profiles");

if (!Directory.Exists(profilesPath))
{
    Directory.CreateDirectory(profilesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(profilesPath),
    RequestPath = "/uploads/profiles"
});
/* =================== FIXED STATIC FILES SERVING ROUTE END =================== */

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
    await db.Database.MigrateAsync();
    await MembersDataSeeder.SeedAsync(db, builder.Configuration);
    await MembersDataSeeder.ResyncEmployerPasswordsAsync(db);
}

Log.Information("PensionVault Members Service starting on port 5001");
app.Run();