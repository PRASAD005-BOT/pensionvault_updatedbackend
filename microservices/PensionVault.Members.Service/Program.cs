using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.Members.Service.Middleware;
using PensionVault.Application.Interfaces;
using PensionVault.Application.Services;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;
using PensionVault.Infrastructure.Repositories;
using PensionVault.Members.Service.ProxyRepositories;

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
});
builder.Services.AddHttpClient<IContributionRepository, HttpContributionRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
});
builder.Services.AddHttpClient<ILedgerRepository, HttpLedgerRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
});
builder.Services.AddHttpClient<IClaimRepository, HttpClaimRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ClaimsUrl"] ?? "http://localhost:5002/");
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<ISchemeService, SchemeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IContributionService, ContributionService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddScoped<PensionVault.Infrastructure.Filters.AuditLogFilter>(sp =>
    new PensionVault.Infrastructure.Filters.AuditLogFilter(
        sp.GetRequiredService<MembersDbContext>()));
builder.Services.AddControllers(opts =>
    {
        opts.Filters.AddService<PensionVault.Infrastructure.Filters.AuditLogFilter>();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
    await db.Database.MigrateAsync();
    await PensionVault.Infrastructure.Seeders.DataSeeder.SeedAsync(db, builder.Configuration);
}

Log.Information("PensionVault Members Service starting on port 7001");
app.Run();

