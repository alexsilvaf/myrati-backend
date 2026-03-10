using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
            var productPlanTable = GetTableName(context, typeof(ProductPlan));
            var licenseTable = GetTableName(context, typeof(License));
            var sprintTable = GetTableName(context, typeof(ProductSprint));
            var taskTable = GetTableName(context, typeof(ProductTask));

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

            await EnsureSprintTableAsync(connection, context.Database.ProviderName, productTable, sprintTable, cancellationToken);
            await EnsureTaskTableAsync(connection, context.Database.ProviderName, productTable, sprintTable, taskTable, cancellationToken);
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
