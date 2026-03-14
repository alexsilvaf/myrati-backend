using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Myrati.Domain.Auditing;
using Myrati.Domain.Compliance;
using Myrati.Domain.Costs;
using Myrati.Domain.Dashboard;
using Myrati.Domain.Identity;
using Myrati.Domain.Notifications;
using Myrati.Domain.Products;
using Myrati.Infrastructure.Persistence;

namespace Myrati.Infrastructure.Seeding;

internal static class DatabaseSchemaCompatibilityUpgrader
{
    public static async Task ApplyAsync(MyratiDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var productTable = GetTableName(context, typeof(Product));
            var auditLogTable = GetTableName(context, typeof(AuditLog));
            var adminUserTable = GetTableName(context, typeof(AdminUser));
            var passwordSetupTokenTable = GetTableName(context, typeof(PasswordSetupToken));
            var productPlanTable = GetTableName(context, typeof(ProductPlan));
            var productCollaboratorTable = GetTableName(context, typeof(ProductCollaborator));
            var productExpenseTable = GetTableName(context, typeof(ProductExpense));
            var licenseTable = GetTableName(context, typeof(License));
            var sprintTable = GetTableName(context, typeof(ProductSprint));
            var taskTable = GetTableName(context, typeof(ProductTask));
            var companyCostTable = GetTableName(context, typeof(CompanyCost));
            var adminNotificationTable = GetTableName(context, typeof(AdminNotification));
            var activityFeedTable = GetTableName(context, typeof(ActivityFeedItem));
            var dataSubjectRequestTable = GetTableName(context, typeof(DataSubjectRequest));
            var processingActivityTable = GetTableName(context, typeof(ProcessingActivityRecord));
            var securityIncidentTable = GetTableName(context, typeof(SecurityIncidentRecord));

            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                "SalesStrategy",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "SalesStrategy" TEXT NOT NULL DEFAULT 'subscription';""",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "SalesStrategy" character varying(40) NOT NULL DEFAULT 'subscription';""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                "ProductionDeploys",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "ProductionDeploys" INTEGER NOT NULL DEFAULT 0;""",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "ProductionDeploys" integer NOT NULL DEFAULT 0;""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                "DevSprintsSinceLastDeploy",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "DevSprintsSinceLastDeploy" INTEGER NOT NULL DEFAULT 0;""",
                $"""ALTER TABLE "{productTable}" ADD COLUMN "DevSprintsSinceLastDeploy" integer NOT NULL DEFAULT 0;""",
                cancellationToken);
            await EnsureColumnRemovedAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                "Version",
                $"""ALTER TABLE "{productTable}" DROP COLUMN "Version";""",
                $"""ALTER TABLE "{productTable}" DROP COLUMN IF EXISTS "Version";""",
                cancellationToken);

            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productPlanTable,
                "DevelopmentCost",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "DevelopmentCost" NUMERIC NULL;""",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "DevelopmentCost" numeric(18,2) NULL;""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productPlanTable,
                "MaintenanceCost",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "MaintenanceCost" NUMERIC NULL;""",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "MaintenanceCost" numeric(18,2) NULL;""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productPlanTable,
                "RevenueSharePercent",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "RevenueSharePercent" NUMERIC NULL;""",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "RevenueSharePercent" numeric(5,2) NULL;""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                productPlanTable,
                "MaintenanceProfitMargin",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "MaintenanceProfitMargin" NUMERIC NULL;""",
                $"""ALTER TABLE "{productPlanTable}" ADD COLUMN "MaintenanceProfitMargin" numeric(5,2) NULL;""",
                cancellationToken);
            await EnsureColumnNullableAsync(
                connection,
                context.Database.ProviderName,
                productPlanTable,
                "MaxUsers",
                $"""ALTER TABLE "{productPlanTable}" ALTER COLUMN "MaxUsers" DROP NOT NULL;""",
                cancellationToken);

            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                licenseTable,
                "DevelopmentCost",
                $"""ALTER TABLE "{licenseTable}" ADD COLUMN "DevelopmentCost" NUMERIC NULL;""",
                $"""ALTER TABLE "{licenseTable}" ADD COLUMN "DevelopmentCost" numeric(18,2) NULL;""",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                context.Database.ProviderName,
                licenseTable,
                "RevenueSharePercent",
                $"""ALTER TABLE "{licenseTable}" ADD COLUMN "RevenueSharePercent" NUMERIC NULL;""",
                $"""ALTER TABLE "{licenseTable}" ADD COLUMN "RevenueSharePercent" numeric(5,2) NULL;""",
                cancellationToken);
            await EnsureColumnNullableAsync(
                connection,
                context.Database.ProviderName,
                licenseTable,
                "MaxUsers",
                $"""ALTER TABLE "{licenseTable}" ALTER COLUMN "MaxUsers" DROP NOT NULL;""",
                cancellationToken);

            await EnsureAuditLogTableAsync(
                connection,
                context.Database.ProviderName,
                auditLogTable,
                cancellationToken);
            await EnsureProductCollaboratorTableAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                adminUserTable,
                productCollaboratorTable,
                cancellationToken);
            await EnsurePasswordSetupTokenTableAsync(
                connection,
                context.Database.ProviderName,
                adminUserTable,
                passwordSetupTokenTable,
                cancellationToken);
            await EnsureSprintTableAsync(connection, context.Database.ProviderName, productTable, sprintTable, cancellationToken);
            await EnsureTaskTableAsync(connection, context.Database.ProviderName, productTable, sprintTable, taskTable, cancellationToken);
            await EnsureProductExpenseTableAsync(
                connection,
                context.Database.ProviderName,
                productTable,
                productExpenseTable,
                cancellationToken);
            await EnsureCompanyCostTableAsync(
                connection,
                context.Database.ProviderName,
                companyCostTable,
                cancellationToken);
            await EnsureAdminNotificationTableAsync(
                connection,
                context.Database.ProviderName,
                adminUserTable,
                adminNotificationTable,
                cancellationToken);
            await EnsureDataSubjectRequestTableAsync(
                connection,
                context.Database.ProviderName,
                dataSubjectRequestTable,
                cancellationToken);
            await EnsureProcessingActivityTableAsync(
                connection,
                context.Database.ProviderName,
                processingActivityTable,
                cancellationToken);
            await EnsureSecurityIncidentTableAsync(
                connection,
                context.Database.ProviderName,
                securityIncidentTable,
                cancellationToken);
            await RemoveDeprecatedNotificationsAsync(
                connection,
                adminNotificationTable,
                cancellationToken);
            await RemoveDeprecatedActivityFeedItemsAsync(
                connection,
                activityFeedTable,
                cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string GetTableName(MyratiDbContext context, Type entityType)
    {
        var tableName = context.Model.FindEntityType(entityType)?.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new InvalidOperationException($"Tabela não encontrada para '{entityType.Name}'.");
        }

        return tableName;
    }

    private static async Task EnsureColumnAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        string sqliteSql,
        string postgresSql,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, providerName, tableName, columnName, cancellationToken))
        {
            return;
        }

        var sql = IsSqlite(providerName) ? sqliteSql : postgresSql;
        await ExecuteNonQueryAsync(connection, sql, cancellationToken);
    }

    private static async Task EnsureColumnNullableAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        string postgresSql,
        CancellationToken cancellationToken)
    {
        if (IsSqlite(providerName) || await ColumnIsNullableAsync(connection, providerName, tableName, columnName, cancellationToken))
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, postgresSql, cancellationToken);
    }

    private static async Task EnsureColumnRemovedAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        string sqliteSql,
        string postgresSql,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, providerName, tableName, columnName, cancellationToken))
        {
            return;
        }

        var sql = IsSqlite(providerName) ? sqliteSql : postgresSql;
        await ExecuteNonQueryAsync(connection, sql, cancellationToken);
    }

    private static async Task EnsureProductCollaboratorTableAsync(
        DbConnection connection,
        string? providerName,
        string productTable,
        string adminUserTable,
        string collaboratorTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, collaboratorTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{collaboratorTable}" (
    "ProductId" TEXT NOT NULL,
    "MemberId" TEXT NOT NULL,
    "AddedDate" TEXT NOT NULL,
    "TasksView" INTEGER NOT NULL,
    "TasksCreate" INTEGER NOT NULL,
    "TasksEdit" INTEGER NOT NULL,
    "TasksDelete" INTEGER NOT NULL,
    "SprintsView" INTEGER NOT NULL,
    "SprintsCreate" INTEGER NOT NULL,
    "SprintsEdit" INTEGER NOT NULL,
    "SprintsDelete" INTEGER NOT NULL,
    "LicensesView" INTEGER NOT NULL,
    "LicensesCreate" INTEGER NOT NULL,
    "LicensesEdit" INTEGER NOT NULL,
    "LicensesDelete" INTEGER NOT NULL,
    "PlansView" INTEGER NOT NULL,
    "PlansCreate" INTEGER NOT NULL,
    "PlansEdit" INTEGER NOT NULL,
    "PlansDelete" INTEGER NOT NULL,
    "ProductView" INTEGER NOT NULL,
    "ProductCreate" INTEGER NOT NULL,
    "ProductEdit" INTEGER NOT NULL,
    "ProductDelete" INTEGER NOT NULL,
    CONSTRAINT "PK_{collaboratorTable}" PRIMARY KEY ("ProductId", "MemberId"),
    CONSTRAINT "FK_{collaboratorTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_{collaboratorTable}_{adminUserTable}_MemberId" FOREIGN KEY ("MemberId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
"""
                : $"""
CREATE TABLE "{collaboratorTable}" (
    "ProductId" character varying(40) NOT NULL,
    "MemberId" character varying(40) NOT NULL,
    "AddedDate" date NOT NULL,
    "TasksView" boolean NOT NULL,
    "TasksCreate" boolean NOT NULL,
    "TasksEdit" boolean NOT NULL,
    "TasksDelete" boolean NOT NULL,
    "SprintsView" boolean NOT NULL,
    "SprintsCreate" boolean NOT NULL,
    "SprintsEdit" boolean NOT NULL,
    "SprintsDelete" boolean NOT NULL,
    "LicensesView" boolean NOT NULL,
    "LicensesCreate" boolean NOT NULL,
    "LicensesEdit" boolean NOT NULL,
    "LicensesDelete" boolean NOT NULL,
    "PlansView" boolean NOT NULL,
    "PlansCreate" boolean NOT NULL,
    "PlansEdit" boolean NOT NULL,
    "PlansDelete" boolean NOT NULL,
    "ProductView" boolean NOT NULL,
    "ProductCreate" boolean NOT NULL,
    "ProductEdit" boolean NOT NULL,
    "ProductDelete" boolean NOT NULL,
    CONSTRAINT "PK_{collaboratorTable}" PRIMARY KEY ("ProductId", "MemberId"),
    CONSTRAINT "FK_{collaboratorTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_{collaboratorTable}_{adminUserTable}_MemberId" FOREIGN KEY ("MemberId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await EnsureMirroredPermissionColumnAsync(
            connection,
            providerName,
            collaboratorTable,
            "PlansView",
            "ProductView",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansView" INTEGER NOT NULL DEFAULT 0;""",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansView" boolean NOT NULL DEFAULT false;""",
            cancellationToken);
        await EnsureMirroredPermissionColumnAsync(
            connection,
            providerName,
            collaboratorTable,
            "PlansCreate",
            "ProductCreate",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansCreate" INTEGER NOT NULL DEFAULT 0;""",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansCreate" boolean NOT NULL DEFAULT false;""",
            cancellationToken);
        await EnsureMirroredPermissionColumnAsync(
            connection,
            providerName,
            collaboratorTable,
            "PlansEdit",
            "ProductEdit",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansEdit" INTEGER NOT NULL DEFAULT 0;""",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansEdit" boolean NOT NULL DEFAULT false;""",
            cancellationToken);
        await EnsureMirroredPermissionColumnAsync(
            connection,
            providerName,
            collaboratorTable,
            "PlansDelete",
            "ProductDelete",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansDelete" INTEGER NOT NULL DEFAULT 0;""",
            $"""ALTER TABLE "{collaboratorTable}" ADD COLUMN "PlansDelete" boolean NOT NULL DEFAULT false;""",
            cancellationToken);

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{collaboratorTable}_MemberId" ON "{collaboratorTable}" ("MemberId");""",
            cancellationToken);
    }

    private static async Task EnsureMirroredPermissionColumnAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        string sourceColumnName,
        string sqliteSql,
        string postgresSql,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, providerName, tableName, columnName, cancellationToken))
        {
            return;
        }

        await ExecuteNonQueryAsync(connection, IsSqlite(providerName) ? sqliteSql : postgresSql, cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""UPDATE "{tableName}" SET "{columnName}" = "{sourceColumnName}";""",
            cancellationToken);
    }

    private static async Task EnsureAuditLogTableAsync(
        DbConnection connection,
        string? providerName,
        string auditLogTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, auditLogTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{auditLogTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{auditLogTable}" PRIMARY KEY,
    "OccurredAtUtc" TEXT NOT NULL,
    "ServiceName" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "HttpMethod" TEXT NOT NULL,
    "Path" TEXT NOT NULL,
    "ResourceType" TEXT NULL,
    "ResourceId" TEXT NULL,
    "StatusCode" INTEGER NOT NULL,
    "Outcome" TEXT NOT NULL,
    "ActorUserId" TEXT NULL,
    "ActorEmail" TEXT NULL,
    "ActorRole" TEXT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "TraceIdentifier" TEXT NOT NULL
);
"""
                : $"""
CREATE TABLE "{auditLogTable}" (
    "Id" character varying(40) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "ServiceName" character varying(120) NOT NULL,
    "EventType" character varying(200) NOT NULL,
    "HttpMethod" character varying(12) NOT NULL,
    "Path" character varying(240) NOT NULL,
    "ResourceType" character varying(80) NULL,
    "ResourceId" character varying(80) NULL,
    "StatusCode" integer NOT NULL,
    "Outcome" character varying(20) NOT NULL,
    "ActorUserId" character varying(40) NULL,
    "ActorEmail" character varying(160) NULL,
    "ActorRole" character varying(40) NULL,
    "IpAddress" character varying(64) NULL,
    "UserAgent" character varying(512) NULL,
    "TraceIdentifier" character varying(120) NOT NULL,
    CONSTRAINT "PK_{auditLogTable}" PRIMARY KEY ("Id")
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{auditLogTable}_OccurredAtUtc" ON "{auditLogTable}" ("OccurredAtUtc");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{auditLogTable}_ActorEmail_OccurredAtUtc" ON "{auditLogTable}" ("ActorEmail", "OccurredAtUtc");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{auditLogTable}_EventType_OccurredAtUtc" ON "{auditLogTable}" ("EventType", "OccurredAtUtc");""",
            cancellationToken);
    }

    private static async Task EnsurePasswordSetupTokenTableAsync(
        DbConnection connection,
        string? providerName,
        string adminUserTable,
        string passwordSetupTokenTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, passwordSetupTokenTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{passwordSetupTokenTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{passwordSetupTokenTable}" PRIMARY KEY,
    "AdminUserId" TEXT NOT NULL,
    "TokenHash" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "UsedAt" TEXT NULL,
    CONSTRAINT "FK_{passwordSetupTokenTable}_{adminUserTable}_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
"""
                : $"""
CREATE TABLE "{passwordSetupTokenTable}" (
    "Id" character varying(40) NOT NULL,
    "AdminUserId" character varying(40) NOT NULL,
    "TokenHash" character varying(128) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "UsedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_{passwordSetupTokenTable}" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_{passwordSetupTokenTable}_{adminUserTable}_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE UNIQUE INDEX IF NOT EXISTS "IX_{passwordSetupTokenTable}_TokenHash" ON "{passwordSetupTokenTable}" ("TokenHash");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{passwordSetupTokenTable}_AdminUserId" ON "{passwordSetupTokenTable}" ("AdminUserId");""",
            cancellationToken);
    }

    private static async Task EnsureSprintTableAsync(
        DbConnection connection,
        string? providerName,
        string productTable,
        string sprintTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, sprintTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{sprintTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{sprintTable}" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    CONSTRAINT "FK_{sprintTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE
);
"""
                : $"""
CREATE TABLE "{sprintTable}" (
    "Id" character varying(40) NOT NULL,
    "ProductId" character varying(40) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Status" character varying(30) NOT NULL,
    "SortOrder" integer NOT NULL,
    CONSTRAINT "PK_{sprintTable}" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_{sprintTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{sprintTable}_ProductId_SortOrder" ON "{sprintTable}" ("ProductId", "SortOrder");""",
            cancellationToken);
    }

    private static async Task EnsureTaskTableAsync(
        DbConnection connection,
        string? providerName,
        string productTable,
        string sprintTable,
        string taskTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, taskTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{taskTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{taskTable}" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "SprintId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Column" TEXT NOT NULL,
    "Priority" TEXT NOT NULL,
    "Assignee" TEXT NOT NULL,
    "TagsSerialized" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    CONSTRAINT "FK_{taskTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_{taskTable}_{sprintTable}_SprintId" FOREIGN KEY ("SprintId") REFERENCES "{sprintTable}" ("Id") ON DELETE RESTRICT
);
"""
                : $"""
CREATE TABLE "{taskTable}" (
    "Id" character varying(40) NOT NULL,
    "ProductId" character varying(40) NOT NULL,
    "SprintId" character varying(40) NOT NULL,
    "Title" character varying(160) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "Column" character varying(30) NOT NULL,
    "Priority" character varying(20) NOT NULL,
    "Assignee" character varying(160) NOT NULL,
    "TagsSerialized" character varying(4000) NOT NULL,
    "CreatedDate" date NOT NULL,
    "SortOrder" integer NOT NULL,
    CONSTRAINT "PK_{taskTable}" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_{taskTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_{taskTable}_{sprintTable}_SprintId" FOREIGN KEY ("SprintId") REFERENCES "{sprintTable}" ("Id") ON DELETE RESTRICT
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{taskTable}_Board" ON "{taskTable}" ("ProductId", "SprintId", "Column", "SortOrder");""",
            cancellationToken);
    }

    private static async Task EnsureProductExpenseTableAsync(
        DbConnection connection,
        string? providerName,
        string productTable,
        string expenseTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, expenseTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{expenseTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{expenseTable}" PRIMARY KEY,
    "ProductId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Amount" NUMERIC NOT NULL,
    "Recurrence" TEXT NOT NULL,
    "Notes" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    CONSTRAINT "FK_{expenseTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE
);
"""
                : $"""
CREATE TABLE "{expenseTable}" (
    "Id" character varying(40) NOT NULL,
    "ProductId" character varying(40) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Category" character varying(40) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Recurrence" character varying(20) NOT NULL,
    "Notes" character varying(1000) NULL,
    "CreatedDate" date NOT NULL,
    CONSTRAINT "PK_{expenseTable}" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_{expenseTable}_{productTable}_ProductId" FOREIGN KEY ("ProductId") REFERENCES "{productTable}" ("Id") ON DELETE CASCADE
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{expenseTable}_ProductId_CreatedDate" ON "{expenseTable}" ("ProductId", "CreatedDate");""",
            cancellationToken);
    }

    private static async Task EnsureCompanyCostTableAsync(
        DbConnection connection,
        string? providerName,
        string companyCostTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, companyCostTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{companyCostTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{companyCostTable}" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Amount" NUMERIC NOT NULL,
    "Recurrence" TEXT NOT NULL,
    "Vendor" TEXT NOT NULL,
    "StartDate" TEXT NOT NULL,
    "NextBillingDate" TEXT NULL,
    "Status" TEXT NOT NULL
);
"""
                : $"""
CREATE TABLE "{companyCostTable}" (
    "Id" character varying(40) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Category" character varying(40) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Recurrence" character varying(20) NOT NULL,
    "Vendor" character varying(160) NOT NULL,
    "StartDate" date NOT NULL,
    "NextBillingDate" date NULL,
    "Status" character varying(20) NOT NULL,
    CONSTRAINT "PK_{companyCostTable}" PRIMARY KEY ("Id")
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{companyCostTable}_Status_Category" ON "{companyCostTable}" ("Status", "Category");""",
            cancellationToken);
    }

    private static async Task EnsureAdminNotificationTableAsync(
        DbConnection connection,
        string? providerName,
        string adminUserTable,
        string adminNotificationTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, adminNotificationTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{adminNotificationTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{adminNotificationTable}" PRIMARY KEY,
    "RecipientAdminUserId" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "ReadAt" TEXT NULL,
    CONSTRAINT "FK_{adminNotificationTable}_{adminUserTable}_RecipientAdminUserId" FOREIGN KEY ("RecipientAdminUserId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
"""
                : $"""
CREATE TABLE "{adminNotificationTable}" (
    "Id" character varying(40) NOT NULL,
    "RecipientAdminUserId" character varying(40) NOT NULL,
    "EventType" character varying(80) NOT NULL,
    "Title" character varying(160) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "Type" character varying(20) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ReadAt" timestamp with time zone NULL,
    CONSTRAINT "PK_{adminNotificationTable}" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_{adminNotificationTable}_{adminUserTable}_RecipientAdminUserId" FOREIGN KEY ("RecipientAdminUserId") REFERENCES "{adminUserTable}" ("Id") ON DELETE CASCADE
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{adminNotificationTable}_RecipientAdminUserId_CreatedAt" ON "{adminNotificationTable}" ("RecipientAdminUserId", "CreatedAt");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{adminNotificationTable}_RecipientAdminUserId_ReadAt" ON "{adminNotificationTable}" ("RecipientAdminUserId", "ReadAt");""",
            cancellationToken);
    }

    private static async Task EnsureDataSubjectRequestTableAsync(
        DbConnection connection,
        string? providerName,
        string dataSubjectRequestTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, dataSubjectRequestTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{dataSubjectRequestTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{dataSubjectRequestTable}" PRIMARY KEY,
    "SubjectName" TEXT NOT NULL,
    "SubjectEmail" TEXT NOT NULL,
    "SubjectDocument" TEXT NOT NULL,
    "RequestType" TEXT NOT NULL,
    "Channel" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Details" TEXT NOT NULL,
    "ResolutionSummary" TEXT NULL,
    "IdentityVerified" INTEGER NOT NULL,
    "AssignedAdminUserId" TEXT NULL,
    "RequestedAtUtc" TEXT NOT NULL,
    "DueAtUtc" TEXT NOT NULL,
    "AcknowledgedAtUtc" TEXT NULL,
    "CompletedAtUtc" TEXT NULL,
    "UpdatedAtUtc" TEXT NOT NULL
);
"""
                : $"""
CREATE TABLE "{dataSubjectRequestTable}" (
    "Id" character varying(40) NOT NULL,
    "SubjectName" character varying(160) NOT NULL,
    "SubjectEmail" character varying(160) NOT NULL,
    "SubjectDocument" character varying(40) NOT NULL,
    "RequestType" character varying(40) NOT NULL,
    "Channel" character varying(40) NOT NULL,
    "Status" character varying(40) NOT NULL,
    "Details" character varying(3000) NOT NULL,
    "ResolutionSummary" character varying(3000) NULL,
    "IdentityVerified" boolean NOT NULL,
    "AssignedAdminUserId" character varying(40) NULL,
    "RequestedAtUtc" timestamp with time zone NOT NULL,
    "DueAtUtc" timestamp with time zone NOT NULL,
    "AcknowledgedAtUtc" timestamp with time zone NULL,
    "CompletedAtUtc" timestamp with time zone NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_{dataSubjectRequestTable}" PRIMARY KEY ("Id")
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{dataSubjectRequestTable}_RequestedAtUtc" ON "{dataSubjectRequestTable}" ("RequestedAtUtc");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{dataSubjectRequestTable}_Status_DueAtUtc" ON "{dataSubjectRequestTable}" ("Status", "DueAtUtc");""",
            cancellationToken);
    }

    private static async Task EnsureProcessingActivityTableAsync(
        DbConnection connection,
        string? providerName,
        string processingActivityTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, processingActivityTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{processingActivityTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{processingActivityTable}" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "SystemName" TEXT NOT NULL,
    "Purpose" TEXT NOT NULL,
    "LegalBasis" TEXT NOT NULL,
    "DataSubjectCategories" TEXT NOT NULL,
    "PersonalDataCategories" TEXT NOT NULL,
    "SharedWith" TEXT NOT NULL,
    "RetentionPolicy" TEXT NOT NULL,
    "SecurityMeasures" TEXT NOT NULL,
    "OwnerArea" TEXT NOT NULL,
    "InternationalTransfer" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    "ReviewDueAtUtc" TEXT NULL
);
"""
                : $"""
CREATE TABLE "{processingActivityTable}" (
    "Id" character varying(40) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "SystemName" character varying(120) NOT NULL,
    "Purpose" character varying(2000) NOT NULL,
    "LegalBasis" character varying(120) NOT NULL,
    "DataSubjectCategories" character varying(2000) NOT NULL,
    "PersonalDataCategories" character varying(2000) NOT NULL,
    "SharedWith" character varying(2000) NOT NULL,
    "RetentionPolicy" character varying(1000) NOT NULL,
    "SecurityMeasures" character varying(2000) NOT NULL,
    "OwnerArea" character varying(120) NOT NULL,
    "InternationalTransfer" boolean NOT NULL,
    "Status" character varying(40) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "ReviewDueAtUtc" timestamp with time zone NULL,
    CONSTRAINT "PK_{processingActivityTable}" PRIMARY KEY ("Id")
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{processingActivityTable}_Status" ON "{processingActivityTable}" ("Status");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{processingActivityTable}_ReviewDueAtUtc" ON "{processingActivityTable}" ("ReviewDueAtUtc");""",
            cancellationToken);
    }

    private static async Task EnsureSecurityIncidentTableAsync(
        DbConnection connection,
        string? providerName,
        string securityIncidentTable,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, providerName, securityIncidentTable, cancellationToken))
        {
            var sql = IsSqlite(providerName)
                ? $"""
CREATE TABLE "{securityIncidentTable}" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_{securityIncidentTable}" PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "ContainsPersonalData" INTEGER NOT NULL,
    "AffectedDataSummary" TEXT NOT NULL,
    "ImpactSummary" TEXT NOT NULL,
    "MitigationSummary" TEXT NOT NULL,
    "NotifyAnpd" INTEGER NOT NULL,
    "NotifyDataSubjects" INTEGER NOT NULL,
    "AssignedAdminUserId" TEXT NULL,
    "DetectedAtUtc" TEXT NOT NULL,
    "OccurredAtUtc" TEXT NULL,
    "ContainedAtUtc" TEXT NULL,
    "ReportedToAnpdAtUtc" TEXT NULL,
    "ReportedToDataSubjectsAtUtc" TEXT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL
);
"""
                : $"""
CREATE TABLE "{securityIncidentTable}" (
    "Id" character varying(40) NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" character varying(3000) NOT NULL,
    "Severity" character varying(20) NOT NULL,
    "Status" character varying(40) NOT NULL,
    "ContainsPersonalData" boolean NOT NULL,
    "AffectedDataSummary" character varying(2000) NOT NULL,
    "ImpactSummary" character varying(2000) NOT NULL,
    "MitigationSummary" character varying(2000) NOT NULL,
    "NotifyAnpd" boolean NOT NULL,
    "NotifyDataSubjects" boolean NOT NULL,
    "AssignedAdminUserId" character varying(40) NULL,
    "DetectedAtUtc" timestamp with time zone NOT NULL,
    "OccurredAtUtc" timestamp with time zone NULL,
    "ContainedAtUtc" timestamp with time zone NULL,
    "ReportedToAnpdAtUtc" timestamp with time zone NULL,
    "ReportedToDataSubjectsAtUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_{securityIncidentTable}" PRIMARY KEY ("Id")
);
""";

            await ExecuteNonQueryAsync(connection, sql, cancellationToken);
        }

        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{securityIncidentTable}_DetectedAtUtc" ON "{securityIncidentTable}" ("DetectedAtUtc");""",
            cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            $"""CREATE INDEX IF NOT EXISTS "IX_{securityIncidentTable}_Status_ContainsPersonalData" ON "{securityIncidentTable}" ("Status", "ContainsPersonalData");""",
            cancellationToken);
    }

    private static async Task RemoveDeprecatedNotificationsAsync(
        DbConnection connection,
        string adminNotificationTable,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            $"""DELETE FROM "{adminNotificationTable}" WHERE "EventType" = 'auth.login';""",
            cancellationToken);
    }

    private static async Task RemoveDeprecatedActivityFeedItemsAsync(
        DbConnection connection,
        string activityFeedTable,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            $"""DELETE FROM "{activityFeedTable}" WHERE "Action" = 'Novo login administrativo';""",
            cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = IsSqlite(providerName)
            ? "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1;"
            : "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @name LIMIT 1;";

        return await ExistsAsync(connection, sql, cancellationToken, ("@name", tableName));
    }

    private static async Task<bool> ColumnExistsAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var sql = IsSqlite(providerName)
            ? $"""SELECT 1 FROM pragma_table_info('{tableName}') WHERE name = @column LIMIT 1;"""
            : "SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table AND column_name = @column LIMIT 1;";

        return IsSqlite(providerName)
            ? await ExistsAsync(connection, sql, cancellationToken, ("@column", columnName))
            : await ExistsAsync(connection, sql, cancellationToken, ("@table", tableName), ("@column", columnName));
    }

    private static async Task<bool> ColumnIsNullableAsync(
        DbConnection connection,
        string? providerName,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        if (IsSqlite(providerName))
        {
            return true;
        }

        const string sql = """
SELECT CASE
    WHEN is_nullable = 'YES' THEN 1
    ELSE 0
END
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = @table
  AND column_name = @column
LIMIT 1;
""";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@table";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        var columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@column";
        columnParameter.Value = columnName;
        command.Parameters.Add(columnParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result switch
        {
            1 => true,
            long longValue when longValue == 1 => true,
            decimal decimalValue when decimalValue == 1 => true,
            _ => false
        };
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value;
            command.Parameters.Add(dbParameter);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsSqlite(string? providerName) =>
        providerName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
