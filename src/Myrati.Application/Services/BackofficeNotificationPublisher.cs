using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Domain.Dashboard;
using Myrati.Domain.Notifications;

namespace Myrati.Application.Services;

public sealed class BackofficeNotificationPublisher(
    IMyratiDbContext dbContext) : IBackofficeNotificationPublisher
{
    private const int MaxActivityItems = 20;

    public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var blueprint = BuildBlueprint(eventType, payload);
        if (blueprint is null)
        {
            return;
        }

        await AddActivityFeedItemAsync(blueprint, cancellationToken);

        var settings = await dbContext.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings?.PushNotifications == false)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var recipients = await ResolveRecipientsAsync(eventType, blueprint.ProductId, blueprint.TargetAdminUserId, cancellationToken);
        if (recipients.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingIds = await dbContext.AdminNotifications
            .Select(notification => notification.Id)
            .ToListAsync(cancellationToken);

        foreach (var recipientId in recipients)
        {
            var notificationId = IdGenerator.NextPrefixedId("NTF-", existingIds);
            existingIds.Add(notificationId);

            await dbContext.AddAsync(new AdminNotification
            {
                Id = notificationId,
                RecipientAdminUserId = recipientId,
                EventType = eventType,
                Title = blueprint.Title,
                Description = blueprint.Description,
                Type = blueprint.Type,
                CreatedAt = blueprint.CreatedAt
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddActivityFeedItemAsync(NotificationBlueprint blueprint, CancellationToken cancellationToken)
    {
        var activities = await dbContext.ActivityFeedItems
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var activity in activities)
        {
            activity.SortOrder += 1;
            dbContext.Update(activity);
        }

        var existingIds = activities.Select(activity => activity.Id).ToList();
        var activityId = IdGenerator.NextPrefixedId("DA-", existingIds);

        await dbContext.AddAsync(new ActivityFeedItem
        {
            Id = activityId,
            Action = blueprint.Title,
            Description = blueprint.Description,
            TimeDisplay = "Agora",
            Type = MapActivityType(blueprint.EventType, blueprint.Type),
            SortOrder = 1
        }, cancellationToken);

        foreach (var staleActivity in activities.Where(activity => activity.SortOrder > MaxActivityItems))
        {
            dbContext.Remove(staleActivity);
        }
    }

    private async Task<IReadOnlyCollection<string>> ResolveRecipientsAsync(
        string eventType,
        string? productId,
        string? targetAdminUserId,
        CancellationToken cancellationToken)
    {
        var activeUsers = await dbContext.AdminUsers
            .Where(user => user.Status == "Ativo")
            .ToListAsync(cancellationToken);

        var developerIds = productId is null
            ? []
            : await dbContext.ProductCollaborators
                .Where(collaborator => collaborator.ProductId == productId)
                .Select(collaborator => collaborator.MemberId)
                .Distinct()
                .ToListAsync(cancellationToken);

        var recipients = new HashSet<string>(StringComparer.Ordinal);

        void AddAdmins() =>
            recipients.UnionWith(activeUsers
                .Where(user => user.Role is "Super Admin" or "Admin")
                .Select(user => user.Id));

        void AddVendors() =>
            recipients.UnionWith(activeUsers
                .Where(user => user.Role == "Vendedor")
                .Select(user => user.Id));

        void AddScopedDevelopers() =>
            recipients.UnionWith(activeUsers
                .Where(user => user.Role == "Desenvolvedor" && developerIds.Contains(user.Id, StringComparer.Ordinal))
                .Select(user => user.Id));

        switch (eventType)
        {
            case "auth.password-setup-completed":
            case "settings.updated":
            case "apikey.created":
            case "apikey.rotated":
            case "apikey.toggled":
            case "apikey.deleted":
            case "team-member.created":
            case "team-member.updated":
            case "team-member.deleted":
                AddAdmins();
                break;

            case "profile.updated":
            case "profile.password-changed":
            case "profile.session-revoked":
                AddAdmins();
                if (!string.IsNullOrWhiteSpace(targetAdminUserId))
                {
                    recipients.Add(targetAdminUserId);
                }
                break;

            case "client.created":
            case "client.updated":
            case "client.deleted":
            case "contact.received":
                AddAdmins();
                AddVendors();
                break;

            case "license.created":
            case "license.updated":
            case "license.suspended":
            case "license.reactivated":
            case "license.deleted":
                AddAdmins();
                AddVendors();
                AddScopedDevelopers();
                break;

            case "product.created":
            case "product.updated":
            case "product.deleted":
            case "sprint.created":
            case "sprint.updated":
            case "sprint.deleted":
            case "task.created":
            case "task.updated":
            case "task.moved":
            case "task.deleted":
                AddAdmins();
                AddScopedDevelopers();
                break;

            default:
                AddAdmins();
                break;
        }

        return recipients.ToArray();
    }

    private NotificationBlueprint? BuildBlueprint(string eventType, object payload)
    {
        var createdAt = DateTimeOffset.UtcNow;
        return eventType switch
        {
            "auth.password-setup-completed" => new NotificationBlueprint(
                eventType,
                "Senha de acesso definida",
                $"{GetString(payload, "Name", "name") ?? "Usuario"} concluiu o primeiro acesso.",
                "success",
                null,
                GetString(payload, "Id", "id"),
                createdAt),
            "client.created" => CreateClientBlueprint(eventType, "Cliente criado", payload, "success", createdAt),
            "client.updated" => CreateClientBlueprint(eventType, "Cliente atualizado", payload, "info", createdAt),
            "client.deleted" => CreateClientBlueprint(eventType, "Cliente removido", payload, "warning", createdAt),
            "product.created" => CreateProductBlueprint(eventType, "Produto criado", payload, "success", createdAt),
            "product.updated" => CreateProductBlueprint(eventType, "Produto atualizado", payload, "info", createdAt),
            "product.deleted" => CreateProductBlueprint(eventType, "Produto removido", payload, "warning", createdAt),
            "license.created" => CreateLicenseBlueprint(eventType, "Licenca criada", payload, "success", createdAt),
            "license.updated" => CreateLicenseBlueprint(eventType, "Licenca atualizada", payload, "info", createdAt),
            "license.suspended" => CreateLicenseBlueprint(eventType, "Licenca suspensa", payload, "warning", createdAt),
            "license.reactivated" => CreateLicenseBlueprint(eventType, "Licenca reativada", payload, "success", createdAt),
            "license.deleted" => new NotificationBlueprint(
                eventType,
                "Licenca removida",
                $"Licenca {GetString(payload, "LicenseId", "licenseId") ?? string.Empty} foi removida.",
                "warning",
                GetString(payload, "ProductId", "productId"),
                null,
                createdAt),
            "settings.updated" => new NotificationBlueprint(
                eventType,
                "Configuracoes atualizadas",
                "As preferencias gerais da plataforma foram atualizadas.",
                "info",
                null,
                null,
                createdAt),
            "apikey.created" => CreateApiKeyBlueprint(eventType, "Chave de API criada", payload, "success", createdAt),
            "apikey.rotated" => CreateApiKeyBlueprint(eventType, "Chave de API renovada", payload, "info", createdAt),
            "apikey.toggled" => CreateApiKeyBlueprint(eventType, "Chave de API alterada", payload, "info", createdAt),
            "apikey.deleted" => CreateApiKeyBlueprint(eventType, "Chave de API removida", payload, "warning", createdAt),
            "team-member.created" => CreateTeamMemberBlueprint(eventType, "Convite enviado", payload, "success", createdAt),
            "team-member.updated" => CreateTeamMemberBlueprint(eventType, "Membro atualizado", payload, "info", createdAt),
            "team-member.deleted" => CreateTeamMemberBlueprint(eventType, "Membro removido", payload, "warning", createdAt),
            "profile.updated" => CreateProfileBlueprint(eventType, "Perfil atualizado", payload, "info", createdAt),
            "profile.password-changed" => CreateProfileBlueprint(eventType, "Senha alterada", payload, "success", createdAt),
            "profile.session-revoked" => CreateProfileBlueprint(eventType, "Sessao revogada", payload, "warning", createdAt),
            "contact.received" => new NotificationBlueprint(
                eventType,
                "Novo contato recebido",
                $"{GetString(payload, "Name", "name") ?? "Lead"} entrou em contato pelo site.",
                "success",
                null,
                null,
                createdAt),
            "sprint.created" => CreateSprintBlueprint(eventType, "Sprint criada", payload, "success", createdAt),
            "sprint.updated" => CreateSprintBlueprint(eventType, "Sprint atualizada", payload, "info", createdAt),
            "sprint.deleted" => CreateSprintBlueprint(eventType, "Sprint removida", payload, "warning", createdAt),
            "task.created" => CreateTaskBlueprint(eventType, "Tarefa criada", payload, "success", createdAt),
            "task.updated" => CreateTaskBlueprint(eventType, "Tarefa atualizada", payload, "info", createdAt),
            "task.moved" => CreateTaskBlueprint(eventType, "Tarefa movida", payload, "info", createdAt),
            "task.deleted" => CreateTaskBlueprint(eventType, "Tarefa removida", payload, "warning", createdAt),
            _ => null
        };
    }

    private static NotificationBlueprint CreateClientBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Company", "company", "Name", "name") ?? "Cliente"} foi sincronizado.",
            type,
            null,
            null,
            createdAt);

    private static NotificationBlueprint CreateProductBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Name", "name") ?? "Produto"} foi sincronizado.",
            type,
            GetString(payload, "ProductId", "productId", "Id", "id"),
            null,
            createdAt);

    private static NotificationBlueprint CreateLicenseBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "ClientName", "clientName") ?? "Cliente"} - {GetString(payload, "Plan", "plan") ?? "Plano"}.",
            type,
            GetString(payload, "ProductId", "productId"),
            null,
            createdAt);

    private static NotificationBlueprint CreateApiKeyBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Label", "label") ?? "Chave"} recebeu alteracoes.",
            type,
            null,
            null,
            createdAt);

    private static NotificationBlueprint CreateTeamMemberBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Name", "name", "Email", "email") ?? "Membro"} foi sincronizado.",
            type,
            null,
            GetString(payload, "Id", "id"),
            createdAt);

    private static NotificationBlueprint CreateProfileBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            eventType switch
            {
                "profile.password-changed" => $"A senha da conta {GetString(payload, "Email", "email") ?? string.Empty} foi alterada.",
                "profile.session-revoked" => $"A sessao {GetString(payload, "SessionId", "sessionId") ?? string.Empty} foi encerrada.",
                _ => $"{GetString(payload, "Name", "name", "Email", "email") ?? "Perfil"} foi atualizado."
            },
            type,
            null,
            GetString(payload, "Id", "id"),
            createdAt);

    private static NotificationBlueprint CreateSprintBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Name", "name") ?? "Sprint"} foi sincronizada.",
            type,
            GetString(payload, "ProductId", "productId"),
            null,
            createdAt);

    private static NotificationBlueprint CreateTaskBlueprint(
        string eventType,
        string title,
        object payload,
        string type,
        DateTimeOffset createdAt) =>
        new(
            eventType,
            title,
            $"{GetString(payload, "Title", "title") ?? "Tarefa"} foi sincronizada.",
            type,
            GetString(payload, "ProductId", "productId"),
            null,
            createdAt);

    private static string MapActivityType(string eventType, string notificationType) =>
        eventType switch
        {
            "client.created" or "client.updated" or "client.deleted" => "client",
            "license.created" or "product.created" or "task.created" or "sprint.created" or "contact.received" => "create",
            "license.reactivated" => "renew",
            "license.suspended" or "license.deleted" or "product.deleted" or "task.deleted" or "sprint.deleted" => "suspend",
            _ when notificationType == "warning" || notificationType == "error" => "suspend",
            _ when notificationType == "success" => "upgrade",
            _ => "upgrade"
        };

    private static string? GetString(object payload, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = payload.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(payload);
            if (value is null)
            {
                continue;
            }

            var stringValue = value.ToString();
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    private sealed record NotificationBlueprint(
        string EventType,
        string Title,
        string Description,
        string Type,
        string? ProductId,
        string? TargetAdminUserId,
        DateTimeOffset CreatedAt);
}
