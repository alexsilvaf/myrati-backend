using Myrati.Domain.Clients;
using Myrati.Domain.Dashboard;
using Myrati.Domain.Identity;
using Myrati.Domain.Products;
using Myrati.Domain.Public;
using Myrati.Domain.Settings;

namespace Myrati.Application.Abstractions;

public interface IMyratiDbContext
{
    IQueryable<AdminUser> AdminUsers { get; }
    IQueryable<ProfileSession> ProfileSessions { get; }
    IQueryable<ProfileActivity> ProfileActivities { get; }
    IQueryable<Product> Products { get; }
    IQueryable<ProductPlan> ProductPlans { get; }
    IQueryable<ProductCollaborator> ProductCollaborators { get; }
    IQueryable<ProductSprint> ProductSprints { get; }
    IQueryable<ProductTask> ProductTasks { get; }
    IQueryable<License> Licenses { get; }
    IQueryable<Client> Clients { get; }
    IQueryable<ConnectedUser> ConnectedUsers { get; }
    IQueryable<CompanySettings> CompanySettings { get; }
    IQueryable<ApiKeyCredential> ApiKeys { get; }
    IQueryable<RevenueSnapshot> RevenueSnapshots { get; }
    IQueryable<ActivityFeedItem> ActivityFeedItems { get; }
    IQueryable<SystemStatusMetadata> SystemStatusMetadata { get; }
    IQueryable<SystemComponentStatus> SystemComponentStatuses { get; }
    IQueryable<SystemIncident> SystemIncidents { get; }
    IQueryable<UptimeSample> UptimeSamples { get; }
    IQueryable<ContactLead> ContactLeads { get; }

    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    void Remove<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
