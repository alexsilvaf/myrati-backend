using Microsoft.Extensions.Configuration;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ComplianceServiceTests
{
    [Fact]
    public async Task CreateDataSubjectRequestAsync_AppliesDefaultDueDate()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = CreateService(scope);

        var before = DateTimeOffset.UtcNow;
        var response = await service.CreateDataSubjectRequestAsync(
            new(
                "Titular Teste",
                "titular@empresa.com",
                "123.456.789-00",
                "Acesso",
                "Email",
                "Solicitou acesso aos dados pessoais tratados.",
                true,
                "TM-001",
                null));
        var after = DateTimeOffset.UtcNow;

        Assert.Equal("Recebida", response.Status);
        Assert.Equal("Admin Master", response.AssignedAdminName);
        Assert.InRange(response.DueAtUtc, before.AddDays(15), after.AddDays(15));
    }

    [Fact]
    public async Task GetSnapshotAsync_ComputesComplianceMetrics()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = CreateService(scope);

        await service.CreateDataSubjectRequestAsync(
            new(
                "Titular Vencendo",
                "vencendo@empresa.com",
                string.Empty,
                "Correcao",
                "Portal",
                "Deseja corrigir dados cadastrais.",
                true,
                null,
                DateTimeOffset.UtcNow.AddDays(1)));
        await service.CreateProcessingActivityAsync(
            new(
                "Suporte ao cliente",
                "Myrati Support",
                "Atendimento e histórico de chamados.",
                "Execucao de contrato",
                "Clientes",
                "Nome, email, telefone, registros de suporte",
                "Equipe interna de suporte",
                "5 anos apos encerramento contratual",
                "Controle de acesso, auditoria e backup criptografado",
                "Operacoes",
                false,
                "Ativa",
                DateTimeOffset.UtcNow.AddDays(10)));
        await service.CreateSecurityIncidentAsync(
            new(
                "Exposicao indevida em anexo",
                "Anexo com dados pessoais enviado para destinatario errado.",
                "Alta",
                "Em investigacao",
                true,
                "Dados cadastrais e historico de atendimento",
                "Risco de acesso indevido a dados de clientes",
                "Bloqueio do chamado, contato com destinatario e revisao do processo",
                true,
                true,
                "TM-001",
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                null));

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(1, snapshot.Metrics.OpenDataSubjectRequests);
        Assert.Equal(1, snapshot.Metrics.DueSoonDataSubjectRequests);
        Assert.Equal(1, snapshot.Metrics.ActiveSecurityIncidents);
        Assert.Equal(1, snapshot.Metrics.ActivitiesNeedingReview);
    }

    [Fact]
    public async Task UpdateSecurityIncidentAsync_InfersContainedAtWhenResolved()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = CreateService(scope);

        var created = await service.CreateSecurityIncidentAsync(
            new(
                "Banco indisponivel",
                "Falha operacional com dado pessoal envolvido.",
                "Media",
                "Aberto",
                true,
                "Dados de autenticacao",
                "Interrupcao temporaria do acesso",
                "Rollback e investigacao",
                false,
                false,
                null,
                DateTimeOffset.UtcNow.AddMinutes(-30),
                null,
                null,
                null,
                null));

        var updated = await service.UpdateSecurityIncidentAsync(
            created.Id,
            new(
                created.Title,
                created.Description,
                "Media",
                "Resolvido",
                true,
                created.AffectedDataSummary,
                created.ImpactSummary,
                "Rollback concluido e monitoração ativada",
                false,
                false,
                null,
                created.DetectedAtUtc,
                created.OccurredAtUtc,
                null,
                null,
                null));

        Assert.NotNull(updated.ContainedAtUtc);
        Assert.Equal("Resolvido", updated.Status);
    }

    private static ComplianceService CreateService(SeededDbContextScope scope)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Compliance:DataSubjectRequestDueDays"] = "15"
            })
            .Build();

        return new ComplianceService(
            scope.Context,
            configuration,
            new CreateDataSubjectRequestRequestValidator(),
            new UpdateDataSubjectRequestRequestValidator(),
            new CreateProcessingActivityRequestValidator(),
            new UpdateProcessingActivityRequestValidator(),
            new CreateSecurityIncidentRequestValidator(),
            new UpdateSecurityIncidentRequestValidator());
    }
}
