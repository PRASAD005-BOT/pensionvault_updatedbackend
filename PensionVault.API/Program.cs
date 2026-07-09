using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using PensionVault.API.Middleware;
using PensionVault.Application.Interfaces;
using PensionVault.Application.Services;
using PensionVault.Domain.Interfaces;
using PensionVault.Infrastructure.Data;
using PensionVault.Infrastructure.Repositories;
using PensionVault.Infrastructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Database (SQL Server) ─────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connString,
        sql => sql.MigrationsAssembly("PensionVault.Infrastructure")));

// ── Repositories (Infrastructure) ────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<IFundAccountRepository, FundAccountRepository>();
builder.Services.AddScoped<IFundSchemeRepository, FundSchemeRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IContributionRepository, ContributionRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IAnnuityRepository, AnnuityRepository>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ── Application Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IAnnuityService, AnnuityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISchemeService, SchemeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ── JWT Authentication ────────────────────────────────────────────────────
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

// ── Controllers ───────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PensionVault.API.Filters.AuditLogFilter>();
})
    .AddJsonOptions(opts => {
        opts.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PensionVault API",
        Version = "v1",
        Description = "Pension & Provident Fund Administration Platform — REST API",
        Contact = new OpenApiContact { Name = "PensionVault Admin", Email = "admin@pensionvault.com" }
    });

    // JWT support in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
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

// ── CORS (for frontend dev) ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Ensure wwwroot exists so UseStaticFiles works even if it was empty
var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

var app = builder.Build();

// ── Apply Migrations & Seed ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db, builder.Configuration);
    
    var nullRetirementMembers = await db.Members.Where(m => m.DateOfRetirement == null).ToListAsync();
    foreach (var m in nullRetirementMembers)
    {
        m.DateOfRetirement = m.DateOfBirth.AddYears(60);
    }
    if (nullRetirementMembers.Any()) await db.SaveChangesAsync();
}

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
    RequestPath = ""
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PensionVault API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "PensionVault API";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return System.Threading.Tasks.Task.CompletedTask;
});

Log.Information("PensionVault API starting on {Url}", "http://localhost:5000");
app.Run();

