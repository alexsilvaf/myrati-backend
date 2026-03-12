var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

var configuredUrls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
var hasHttpsEndpoint = configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase);
if (hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}

app.UseCors("frontend");
app.MapReverseProxy();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
