var builder = WebApplication.CreateBuilder(args);

// Add YARP configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => "PensionVault API Gateway is running on port 7000. Go to /swagger to view service documentations.");

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/members/v1/swagger.json", "Members Service API");
    c.SwaggerEndpoint("/swagger/claims/v1/swagger.json", "Claims Service API");
    c.SwaggerEndpoint("/swagger/annuity/v1/swagger.json", "Annuity Service API");
    c.SwaggerEndpoint("/swagger/contributions/v1/swagger.json", "Contributions Service API");
    c.RoutePrefix = "swagger";
});

app.MapReverseProxy();

app.Run();
