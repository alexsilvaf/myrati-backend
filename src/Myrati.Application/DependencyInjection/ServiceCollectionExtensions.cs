using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Myrati.Application.Services;

namespace Myrati.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        services.AddScoped<IAuditLogsService, AuditLogsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ICostsService, CostsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITransactionsService, TransactionsService>();
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<IClientsService, ClientsService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<INotificationsService, NotificationsService>();
        services.AddScoped<IBackofficeNotificationPublisher, BackofficeNotificationPublisher>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<ILicenseActivationService, LicenseActivationService>();
        return services;
    }
}
