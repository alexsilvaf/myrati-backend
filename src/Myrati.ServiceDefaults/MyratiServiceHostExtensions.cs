using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Myrati.API.Middleware;
using Myrati.API.Security;
using Myrati.API.Controllers;
using Myrati.Application.Abstractions;
using Myrati.Application.DependencyInjection;
using Myrati.Infrastructure.DependencyInjection;
using Myrati.Infrastructure.Seeding;

namespace Myrati.ServiceDefaults;

public static class MyratiServiceHostExtensions
{
    public static void AddMyratiServiceHost(
        this WebApplicationBuilder builder,
        string serviceName,
        params Type[] controllers)
    {
        builder.Configuration
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        builder.Services
            .AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                if (manager.ApplicationParts.All(part => part.Name != typeof(AuthController).Assembly.GetName().Name))
                {
                    manager.ApplicationParts.Add(new AssemblyPart(typeof(AuthController).Assembly));
                }

                manager.FeatureProviders.Add(new AllowedControllersFeatureProvider(controllers));
            });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = serviceName,
                Version = "v1",
                Description = $"API do servico {serviceName} da plataforma Myrati."
            });
        });

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var signingKey = builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key não configurada.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        var accessToken = context.Request.Query["access_token"];

                        if ((path.StartsWithSegments("/api/v1/backoffice/events")
                                || path.StartsWithSegments("/api/v1/backoffice/notifications/stream"))
                            && !string.IsNullOrWhiteSpace(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("BackofficeRead", policy =>
                policy.RequireRole("Super Admin", "Admin", "Vendedor", "Desenvolvedor"));
            options.AddPolicy("PortalRead", policy =>
                policy.RequireRole("Cliente"));
            options.AddPolicy("ProductCreate", policy =>
                policy.RequireRole("Super Admin", "Admin", "Desenvolvedor"));
            options.AddPolicy("BackofficeWrite", policy =>
                policy.RequireRole("Super Admin", "Admin"));
            options.AddPolicy("ProductScopedWrite", policy =>
                policy.RequireRole("Super Admin", "Admin", "Desenvolvedor"));
        });

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

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("public", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        builder.Services.AddHealthChecks();
    }

    public static async Task<WebApplication> BuildMyratiServiceAsync(
        this WebApplicationBuilder builder,
        bool initializeDatabase = false)
    {
        var app = builder.Build();

        if (initializeDatabase)
        {
            await app.Services.InitializeDatabaseAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ApiExceptionMiddleware>();

        var configuredUrls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
        var hasHttpsEndpoint = configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase);
        if (hasHttpsEndpoint)
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();

        return app;
    }
}
