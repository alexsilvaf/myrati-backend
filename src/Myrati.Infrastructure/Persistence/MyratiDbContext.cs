using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Domain.Clients;
using Myrati.Domain.Dashboard;
using Myrati.Domain.Identity;
using Myrati.Domain.Products;
using Myrati.Domain.Public;
using Myrati.Domain.Settings;

namespace Myrati.Infrastructure.Persistence;

public sealed class MyratiDbContext(DbContextOptions<MyratiDbContext> options)
    : DbContext(options), IMyratiDbContext
{
    public DbSet<AdminUser> AdminUsersSet => Set<AdminUser>();
    public DbSet<ProfileSession> ProfileSessionsSet => Set<ProfileSession>();
    public DbSet<ProfileActivity> ProfileActivitiesSet => Set<ProfileActivity>();
    public DbSet<Product> ProductsSet => Set<Product>();
    public DbSet<ProductPlan> ProductPlansSet => Set<ProductPlan>();
    public DbSet<ProductCollaborator> ProductCollaboratorsSet => Set<ProductCollaborator>();
    public DbSet<ProductSprint> ProductSprintsSet => Set<ProductSprint>();
    public DbSet<ProductTask> ProductTasksSet => Set<ProductTask>();
    public DbSet<License> LicensesSet => Set<License>();
    public DbSet<Client> ClientsSet => Set<Client>();
    public DbSet<ConnectedUser> ConnectedUsersSet => Set<ConnectedUser>();
    public DbSet<CompanySettings> CompanySettingsSet => Set<CompanySettings>();
    public DbSet<ApiKeyCredential> ApiKeysSet => Set<ApiKeyCredential>();
    public DbSet<RevenueSnapshot> RevenueSnapshotsSet => Set<RevenueSnapshot>();
    public DbSet<ActivityFeedItem> ActivityFeedItemsSet => Set<ActivityFeedItem>();
    public DbSet<SystemStatusMetadata> SystemStatusMetadataSet => Set<SystemStatusMetadata>();
    public DbSet<SystemComponentStatus> SystemComponentStatusesSet => Set<SystemComponentStatus>();
    public DbSet<SystemIncident> SystemIncidentsSet => Set<SystemIncident>();
    public DbSet<UptimeSample> UptimeSamplesSet => Set<UptimeSample>();
    public DbSet<ContactLead> ContactLeadsSet => Set<ContactLead>();

    IQueryable<AdminUser> IMyratiDbContext.AdminUsers => AdminUsersSet;
    IQueryable<ProfileSession> IMyratiDbContext.ProfileSessions => ProfileSessionsSet;
    IQueryable<ProfileActivity> IMyratiDbContext.ProfileActivities => ProfileActivitiesSet;
    IQueryable<Product> IMyratiDbContext.Products => ProductsSet;
    IQueryable<ProductPlan> IMyratiDbContext.ProductPlans => ProductPlansSet;
    IQueryable<ProductCollaborator> IMyratiDbContext.ProductCollaborators => ProductCollaboratorsSet;
    IQueryable<ProductSprint> IMyratiDbContext.ProductSprints => ProductSprintsSet;
    IQueryable<ProductTask> IMyratiDbContext.ProductTasks => ProductTasksSet;
    IQueryable<License> IMyratiDbContext.Licenses => LicensesSet;
    IQueryable<Client> IMyratiDbContext.Clients => ClientsSet;
    IQueryable<ConnectedUser> IMyratiDbContext.ConnectedUsers => ConnectedUsersSet;
    IQueryable<CompanySettings> IMyratiDbContext.CompanySettings => CompanySettingsSet;
    IQueryable<ApiKeyCredential> IMyratiDbContext.ApiKeys => ApiKeysSet;
    IQueryable<RevenueSnapshot> IMyratiDbContext.RevenueSnapshots => RevenueSnapshotsSet;
    IQueryable<ActivityFeedItem> IMyratiDbContext.ActivityFeedItems => ActivityFeedItemsSet;
    IQueryable<SystemStatusMetadata> IMyratiDbContext.SystemStatusMetadata => SystemStatusMetadataSet;
    IQueryable<SystemComponentStatus> IMyratiDbContext.SystemComponentStatuses => SystemComponentStatusesSet;
    IQueryable<SystemIncident> IMyratiDbContext.SystemIncidents => SystemIncidentsSet;
    IQueryable<UptimeSample> IMyratiDbContext.UptimeSamples => UptimeSamplesSet;
    IQueryable<ContactLead> IMyratiDbContext.ContactLeads => ContactLeadsSet;

    Task IMyratiDbContext.AddAsync<T>(T entity, CancellationToken cancellationToken) =>
        Set<T>().AddAsync(entity, cancellationToken).AsTask();

    void IMyratiDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    void IMyratiDbContext.Update<T>(T entity) => Set<T>().Update(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(160);
            builder.Property(x => x.Email).HasMaxLength(160);
            builder.Property(x => x.Phone).HasMaxLength(25);
            builder.Property(x => x.Role).HasMaxLength(40);
            builder.Property(x => x.Status).HasMaxLength(40);
            builder.Property(x => x.Department).HasMaxLength(100);
            builder.Property(x => x.Location).HasMaxLength(120);
            builder.Property(x => x.PasswordHash).HasMaxLength(200);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasMany(x => x.Sessions)
                .WithOne(x => x.AdminUser)
                .HasForeignKey(x => x.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Activities)
                .WithOne(x => x.AdminUser)
                .HasForeignKey(x => x.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.ProductCollaborations)
                .WithOne(x => x.Member)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileSession>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Location).HasMaxLength(120);
            builder.Property(x => x.LastActiveDisplay).HasMaxLength(80);
        });

        modelBuilder.Entity<ProfileActivity>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Action).HasMaxLength(160);
            builder.Property(x => x.DateDisplay).HasMaxLength(40);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(120);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.Category).HasMaxLength(120);
            builder.Property(x => x.Status).HasMaxLength(40);
            builder.Property(x => x.SalesStrategy).HasMaxLength(40);
            builder.Property(x => x.Version).HasMaxLength(30);
            builder.HasMany(x => x.Plans)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Licenses)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Sprints)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Tasks)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Collaborators)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductPlan>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(60);
            builder.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
            builder.Property(x => x.DevelopmentCost).HasPrecision(18, 2);
            builder.Property(x => x.MaintenanceCost).HasPrecision(18, 2);
            builder.Property(x => x.RevenueSharePercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ProductCollaborator>(builder =>
        {
            builder.HasKey(x => new { x.ProductId, x.MemberId });
            builder.Property(x => x.MemberId).HasMaxLength(40);
            builder.HasIndex(x => x.MemberId);
        });

        modelBuilder.Entity<ProductSprint>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(120);
            builder.Property(x => x.Status).HasMaxLength(30);
            builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        });

        modelBuilder.Entity<ProductTask>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Title).HasMaxLength(160);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Column).HasMaxLength(30);
            builder.Property(x => x.Priority).HasMaxLength(20);
            builder.Property(x => x.Assignee).HasMaxLength(160);
            builder.Property(x => x.TagsSerialized).HasMaxLength(4000);
            builder.HasIndex(x => new { x.ProductId, x.SprintId, x.Column, x.SortOrder });
            builder.HasOne(x => x.Sprint)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.SprintId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<License>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Plan).HasMaxLength(60);
            builder.Property(x => x.Status).HasMaxLength(40);
            builder.Property(x => x.MonthlyValue).HasPrecision(18, 2);
            builder.Property(x => x.DevelopmentCost).HasPrecision(18, 2);
            builder.Property(x => x.RevenueSharePercent).HasPrecision(5, 2);
            builder.HasOne(x => x.Client)
                .WithMany(x => x.Licenses)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.ClientId, x.ProductId, x.Plan });
        });

        modelBuilder.Entity<Client>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(120);
            builder.Property(x => x.Email).HasMaxLength(160);
            builder.Property(x => x.Phone).HasMaxLength(25);
            builder.Property(x => x.Document).HasMaxLength(20);
            builder.Property(x => x.DocumentType).HasMaxLength(10);
            builder.Property(x => x.Company).HasMaxLength(160);
            builder.Property(x => x.Status).HasMaxLength(40);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.Document).IsUnique();
        });

        modelBuilder.Entity<ConnectedUser>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(120);
            builder.Property(x => x.Email).HasMaxLength(160);
            builder.Property(x => x.LastActiveDisplay).HasMaxLength(80);
            builder.Property(x => x.Status).HasMaxLength(40);
            builder.HasOne(x => x.Client)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanySettings>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.CompanyName).HasMaxLength(160);
            builder.Property(x => x.Cnpj).HasMaxLength(20);
            builder.Property(x => x.ContactEmail).HasMaxLength(160);
            builder.Property(x => x.ContactPhone).HasMaxLength(25);
            builder.Property(x => x.Address).HasMaxLength(200);
            builder.Property(x => x.City).HasMaxLength(120);
            builder.Property(x => x.Language).HasMaxLength(20);
            builder.Property(x => x.Timezone).HasMaxLength(50);
            builder.Property(x => x.SessionTimeout).HasMaxLength(20);
        });

        modelBuilder.Entity<ApiKeyCredential>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Label).HasMaxLength(80);
            builder.Property(x => x.Prefix).HasMaxLength(40);
            builder.Property(x => x.Secret).HasMaxLength(64);
        });

        modelBuilder.Entity<RevenueSnapshot>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Month).HasMaxLength(12);
            builder.Property(x => x.Revenue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ActivityFeedItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Action).HasMaxLength(120);
            builder.Property(x => x.Description).HasMaxLength(200);
            builder.Property(x => x.TimeDisplay).HasMaxLength(40);
            builder.Property(x => x.Type).HasMaxLength(30);
        });

        modelBuilder.Entity<SystemStatusMetadata>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.LastUpdatedDisplay).HasMaxLength(80);
        });

        modelBuilder.Entity<SystemComponentStatus>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(120);
            builder.Property(x => x.Status).HasMaxLength(30);
            builder.Property(x => x.Uptime).HasMaxLength(20);
            builder.Property(x => x.ResponseTime).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemIncident>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.DateDisplay).HasMaxLength(30);
            builder.Property(x => x.Title).HasMaxLength(160);
            builder.Property(x => x.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<UptimeSample>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Day).HasMaxLength(10);
            builder.Property(x => x.Percentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ContactLead>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Name).HasMaxLength(160);
            builder.Property(x => x.Email).HasMaxLength(160);
            builder.Property(x => x.Company).HasMaxLength(160);
            builder.Property(x => x.Subject).HasMaxLength(120);
            builder.Property(x => x.Message).HasMaxLength(2000);
        });

        base.OnModelCreating(modelBuilder);
    }
}
