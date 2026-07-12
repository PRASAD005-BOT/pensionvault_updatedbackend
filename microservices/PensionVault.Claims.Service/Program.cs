using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.Claims.Service.Middleware;
using PensionVault.Application.Interfaces;
using PensionVault.Application.Services;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;
using PensionVault.Infrastructure.Repositories;
using PensionVault.Claims.Service.ProxyRepositories;
using PensionVault.Claims.Service.ProxyServices;

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

// Register Local Repositories
builder.Services.AddScoped<IUnitOfWork, GenericUnitOfWork<ClaimsDbContext>>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// Register Proxy Repositories and Services
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IMemberRepository, HttpMemberRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
});
builder.Services.AddHttpClient<IFundAccountRepository, HttpFundAccountRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
});
builder.Services.AddHttpClient<ILedgerRepository, HttpLedgerRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ContributionsUrl"] ?? "http://localhost:5004/");
});
builder.Services.AddHttpClient<INotificationRepository, HttpNotificationRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
});
builder.Services.AddHttpClient<IUserRepository, HttpUserRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
});
builder.Services.AddHttpClient<IMemberService, HttpMemberService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:MembersUrl"] ?? "http://localhost:5001/");
});

// Register Services
builder.Services.AddScoped<IClaimService, ClaimService>();

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
var membersConnStr = builder.Configuration.GetConnectionString("MembersDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddSingleton<PensionVault.Infrastructure.Services.IRawAuditWriter>(
    new PensionVault.Infrastructure.Services.RawAuditWriter(membersConnStr));
builder.Services.AddScoped<PensionVault.Infrastructure.Filters.CrossServiceAuditFilter>();
builder.Services.AddControllers(opts =>
    {
        opts.Filters.AddService<PensionVault.Infrastructure.Filters.CrossServiceAuditFilter>();
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
    await PensionVault.Infrastructure.Seeders.DataSeeder.SeedAsync(db, builder.Configuration);
}

Log.Information("PensionVault Claims Service starting on port 7002");
app.Run();

