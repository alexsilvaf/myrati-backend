using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Domain.Clients;
using Myrati.Domain.Dashboard;
using Myrati.Domain.Identity;
using Myrati.Domain.Products;
using Myrati.Domain.Public;
using Myrati.Domain.Settings;
using Myrati.Infrastructure.Persistence;

namespace Myrati.Infrastructure.Seeding;

public sealed class MyratiDbSeeder(IPasswordHasher passwordHasher)
{
    public async Task SeedAsync(MyratiDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.ProductsSet.AnyAsync(cancellationToken) || await context.AdminUsersSet.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var productSeed in ProductSeeds)
        {
            await context.AddAsync(new Product
            {
                Id = productSeed.Id,
                Name = productSeed.Name,
                Description = productSeed.Description,
                Category = productSeed.Category,
                Status = productSeed.Status,
                SalesStrategy = productSeed.SalesStrategy,
                CreatedDate = ParseDate(productSeed.CreatedDate),
                Version = productSeed.Version
            }, cancellationToken);

            var planIndex = 1;
            foreach (var planSeed in productSeed.Plans)
            {
                await context.AddAsync(new ProductPlan
                {
                    Id = $"{productSeed.Id}-PLAN-{planIndex:D2}",
                    ProductId = productSeed.Id,
                    Name = planSeed.Name,
                    MaxUsers = planSeed.MaxUsers,
                    MonthlyPrice = planSeed.MonthlyPrice,
                    DevelopmentCost = planSeed.DevelopmentCost,
                    MaintenanceCost = planSeed.MaintenanceCost,
                    RevenueSharePercent = planSeed.RevenueSharePercent
                }, cancellationToken);
                planIndex++;
            }
        }

        foreach (var clientSeed in ClientSeeds)
        {
            await context.AddAsync(new Client
            {
                Id = clientSeed.Id,
                Name = clientSeed.Name,
                Email = clientSeed.Email,
                Phone = clientSeed.Phone,
                Document = clientSeed.Document,
                DocumentType = clientSeed.DocumentType,
                Company = clientSeed.Company,
                JoinedDate = ParseDate(clientSeed.JoinedDate),
                Status = clientSeed.Status
            }, cancellationToken);
        }

        var clientIdsByCompany = ClientSeeds.ToDictionary(x => x.Company, x => x.Id);
        var productIdsByName = ProductSeeds.ToDictionary(x => x.Name, x => x.Id);

        foreach (var licenseSeed in LicenseSeeds)
        {
            await context.AddAsync(new License
            {
                Id = licenseSeed.Id,
                ClientId = clientIdsByCompany[licenseSeed.ClientCompany],
                ProductId = productIdsByName[licenseSeed.ProductName],
                Plan = licenseSeed.Plan,
                MaxUsers = licenseSeed.MaxUsers,
                ActiveUsers = licenseSeed.ActiveUsers,
                Status = licenseSeed.Status,
                StartDate = ParseDate(licenseSeed.StartDate),
                ExpiryDate = ParseDate(licenseSeed.ExpiryDate),
                MonthlyValue = licenseSeed.MonthlyValue,
                DevelopmentCost = licenseSeed.DevelopmentCost,
                RevenueSharePercent = licenseSeed.RevenueSharePercent
            }, cancellationToken);
        }

        foreach (var userSeed in ConnectedUserSeeds)
        {
            await context.AddAsync(new ConnectedUser
            {
                Id = userSeed.Id,
                ClientId = clientIdsByCompany[userSeed.ClientCompany],
                ProductId = productIdsByName[userSeed.ProductName],
                Name = userSeed.Name,
                Email = userSeed.Email,
                LastActiveDisplay = userSeed.LastActive,
                Status = userSeed.Status
            }, cancellationToken);
        }

        foreach (var sprintSeed in SprintSeeds)
        {
            await context.AddAsync(new ProductSprint
            {
                Id = sprintSeed.Id,
                ProductId = sprintSeed.ProductId,
                Name = sprintSeed.Name,
                StartDate = ParseDate(sprintSeed.StartDate),
                EndDate = ParseDate(sprintSeed.EndDate),
                Status = sprintSeed.Status,
                SortOrder = sprintSeed.SortOrder
            }, cancellationToken);
        }

        foreach (var taskSeed in TaskSeeds)
        {
            await context.AddAsync(new ProductTask
            {
                Id = taskSeed.Id,
                ProductId = taskSeed.ProductId,
                SprintId = taskSeed.SprintId,
                Title = taskSeed.Title,
                Description = taskSeed.Description,
                Column = taskSeed.Column,
                Priority = taskSeed.Priority,
                Assignee = taskSeed.Assignee,
                TagsSerialized = SerializeTags(taskSeed.Tags),
                CreatedDate = ParseDate(taskSeed.CreatedDate),
                SortOrder = taskSeed.SortOrder
            }, cancellationToken);
        }

        foreach (var snapshotSeed in RevenueSnapshotSeeds)
        {
            await context.AddAsync(new RevenueSnapshot
            {
                Id = snapshotSeed.Id,
                Month = snapshotSeed.Month,
                Revenue = snapshotSeed.Revenue,
                Licenses = snapshotSeed.Licenses,
                SortOrder = snapshotSeed.SortOrder
            }, cancellationToken);
        }

        foreach (var activitySeed in DashboardActivitySeeds)
        {
            await context.AddAsync(new ActivityFeedItem
            {
                Id = activitySeed.Id,
                Action = activitySeed.Action,
                Description = activitySeed.Description,
                TimeDisplay = activitySeed.Time,
                Type = activitySeed.Type,
                SortOrder = activitySeed.SortOrder
            }, cancellationToken);
        }

        await context.AddAsync(new CompanySettings
        {
            Id = "CFG-001",
            CompanyName = "Myrati Tecnologia",
            Cnpj = "12.345.678/0001-90",
            ContactEmail = "contato@myrati.com",
            ContactPhone = "(11) 99999-0000",
            Address = "Rua da Inovação, 123",
            City = "São Paulo / SP",
            Language = "pt-BR",
            Timezone = "America/Sao_Paulo",
            EmailNotifications = true,
            PushNotifications = true,
            LicenseAlerts = true,
            UsageAlerts = true,
            WeeklyReport = false,
            TwoFactorAuth = false,
            SessionTimeout = "30"
        }, cancellationToken);

        foreach (var apiKeySeed in ApiKeySeeds)
        {
            await context.AddAsync(new ApiKeyCredential
            {
                Id = apiKeySeed.Id,
                Label = apiKeySeed.Label,
                Prefix = apiKeySeed.Prefix,
                Secret = apiKeySeed.Secret,
                Active = apiKeySeed.Active,
                CreatedAt = ParseDate(apiKeySeed.CreatedAt)
            }, cancellationToken);
        }

        foreach (var adminSeed in AdminUserSeeds)
        {
            await context.AddAsync(new AdminUser
            {
                Id = adminSeed.Id,
                Name = adminSeed.Name,
                Email = adminSeed.Email,
                Phone = adminSeed.Phone,
                Role = adminSeed.Role,
                Status = adminSeed.Status,
                Department = adminSeed.Department,
                Location = adminSeed.Location,
                PasswordHash = passwordHasher.Hash("Myrati@123"),
                IsPrimaryAccount = adminSeed.IsPrimaryAccount
            }, cancellationToken);
        }

        foreach (var collaboratorSeed in ProductCollaboratorSeeds)
        {
            await context.AddAsync(new ProductCollaborator
            {
                ProductId = collaboratorSeed.ProductId,
                MemberId = collaboratorSeed.MemberId,
                AddedDate = ParseDate(collaboratorSeed.AddedDate),
                TasksView = collaboratorSeed.TasksView,
                TasksCreate = collaboratorSeed.TasksCreate,
                TasksEdit = collaboratorSeed.TasksEdit,
                TasksDelete = collaboratorSeed.TasksDelete,
                SprintsView = collaboratorSeed.SprintsView,
                SprintsCreate = collaboratorSeed.SprintsCreate,
                SprintsEdit = collaboratorSeed.SprintsEdit,
                SprintsDelete = collaboratorSeed.SprintsDelete,
                LicensesView = collaboratorSeed.LicensesView,
                LicensesCreate = collaboratorSeed.LicensesCreate,
                LicensesEdit = collaboratorSeed.LicensesEdit,
                LicensesDelete = collaboratorSeed.LicensesDelete,
                ProductView = collaboratorSeed.ProductView,
                ProductCreate = collaboratorSeed.ProductCreate,
                ProductEdit = collaboratorSeed.ProductEdit,
                ProductDelete = collaboratorSeed.ProductDelete
            }, cancellationToken);
        }

        foreach (var sessionSeed in ProfileSessionSeeds)
        {
            await context.AddAsync(new ProfileSession
            {
                Id = sessionSeed.Id,
                AdminUserId = sessionSeed.AdminUserId,
                Location = sessionSeed.Location,
                LastActiveDisplay = sessionSeed.LastActive,
                IsCurrent = sessionSeed.IsCurrent
            }, cancellationToken);
        }

        foreach (var activitySeed in ProfileActivitySeeds)
        {
            await context.AddAsync(new ProfileActivity
            {
                Id = activitySeed.Id,
                AdminUserId = activitySeed.AdminUserId,
                Action = activitySeed.Action,
                DateDisplay = activitySeed.Date
            }, cancellationToken);
        }

        await context.AddAsync(new SystemStatusMetadata
        {
            Id = "SYS-001",
            LastUpdatedDisplay = "09 Mar 2026 às 14:32 (BRT)"
        }, cancellationToken);

        foreach (var componentSeed in SystemComponentSeeds)
        {
            await context.AddAsync(new SystemComponentStatus
            {
                Id = componentSeed.Id,
                Name = componentSeed.Name,
                Status = componentSeed.Status,
                Uptime = componentSeed.Uptime,
                ResponseTime = componentSeed.ResponseTime,
                SortOrder = componentSeed.SortOrder
            }, cancellationToken);
        }

        foreach (var incidentSeed in SystemIncidentSeeds)
        {
            await context.AddAsync(new SystemIncident
            {
                Id = incidentSeed.Id,
                DateDisplay = incidentSeed.Date,
                Title = incidentSeed.Title,
                Description = incidentSeed.Description,
                Resolved = incidentSeed.Resolved,
                SortOrder = incidentSeed.SortOrder
            }, cancellationToken);
        }

        foreach (var uptimeSeed in UptimeSampleSeeds)
        {
            await context.AddAsync(new UptimeSample
            {
                Id = uptimeSeed.Id,
                Day = uptimeSeed.Day,
                Percentage = uptimeSeed.Pct,
                SortOrder = uptimeSeed.SortOrder
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string SerializeTags(IReadOnlyCollection<string> tags) =>
        System.Text.Json.JsonSerializer.Serialize(tags);

    private static readonly ProductSeed[] ProductSeeds =
    [
        new("PRD-001", "Myrati ERP", "Sistema integrado de gestão empresarial com módulos de financeiro, estoque, compras e vendas.", "Gestão Empresarial", "Ativo", "subscription", "2022-03-15", "4.2.1",
        [
            new PlanSeed("Starter", 10, 890m, null, null, null),
            new PlanSeed("Professional", 50, 3100m, null, null, null),
            new PlanSeed("Enterprise", 200, 12000m, null, null, null)
        ]),
        new("PRD-002", "Myrati CRM", "Plataforma de gestão de relacionamento com clientes, pipeline de vendas e automação de marketing.", "Vendas & Marketing", "Ativo", "subscription", "2023-01-10", "3.1.0",
        [
            new PlanSeed("Starter", 5, 450m, null, null, null),
            new PlanSeed("Professional", 40, 2200m, null, null, null),
            new PlanSeed("Enterprise", 100, 5500m, null, null, null)
        ]),
        new("PRD-003", "Myrati Analytics", "Plataforma de business intelligence com dashboards interativos, relatórios e análise preditiva.", "Inteligência de Dados", "Ativo", "development", "2023-08-20", "2.0.5",
        [
            new PlanSeed("Essencial", 15, 0m, 42000m, 1500m, null),
            new PlanSeed("Completo", 100, 0m, 85000m, 3200m, null)
        ]),
        new("PRD-004", "Myrati HRM", "Sistema de gestão de recursos humanos com folha de pagamento, ponto eletrônico e recrutamento.", "Recursos Humanos", "Em desenvolvimento", "revenue_share", "2025-11-01", "0.9.0-beta",
        [
            new PlanSeed("Padrão", 30, 0m, null, 2000m, 5m),
            new PlanSeed("Premium", 100, 0m, null, 4500m, 3.5m)
        ])
    ];

    private static readonly ClientSeed[] ClientSeeds =
    [
        new("CLI-001", "Carlos Silva", "carlos@techcorp.com.br", "(11) 98765-4321", "12.345.678/0001-90", "CNPJ", "TechCorp Brasil", "2023-05-12", "Ativo"),
        new("CLI-002", "Ana Oliveira", "ana@innovadigital.com", "(21) 97654-3210", "23.456.789/0001-01", "CNPJ", "InnovaDigital", "2024-01-20", "Ativo"),
        new("CLI-003", "Roberto Santos", "roberto@solucoesti.com", "(31) 96543-2109", "345.678.901-23", "CPF", "SoluçõesTI", "2024-06-10", "Ativo"),
        new("CLI-004", "Fernanda Lima", "fernanda@dataflow.com", "(41) 95432-1098", "34.567.890/0001-12", "CNPJ", "DataFlow Ltda", "2023-11-01", "Ativo"),
        new("CLI-005", "Pedro Almeida", "pedro@nexgen.com", "(51) 94321-0987", "456.789.012-34", "CPF", "NexGen Solutions", "2024-03-15", "Inativo"),
        new("CLI-006", "Mariana Costa", "mariana@cloudbase.com", "(61) 93210-9876", "45.678.901/0001-23", "CNPJ", "CloudBase SA", "2024-02-28", "Ativo"),
        new("CLI-007", "Lucas Ferreira", "lucas@greentech.com", "(71) 92109-8765", "567.890.123-45", "CPF", "GreenTech Inc", "2024-04-01", "Inativo"),
        new("CLI-008", "Juliana Mendes", "juliana@digitalwave.com", "(81) 91098-7654", "56.789.012/0001-34", "CNPJ", "DigitalWave", "2023-08-20", "Ativo"),
        new("CLI-009", "Rafael Rodrigues", "rafael@financehub.com", "(91) 90987-6543", "67.890.123/0001-45", "CNPJ", "FinanceHub", "2023-09-01", "Ativo"),
        new("CLI-010", "Beatriz Souza", "beatriz@startupbox.com", "(11) 99876-5432", "678.901.234-56", "CPF", "StartupBox", "2025-12-15", "Ativo"),
        new("CLI-011", "Bruno Cardoso", "bruno@megasoft.com", "(11) 95555-4000", "78.901.234/0001-56", "CNPJ", "MegaSoft", "2024-05-15", "Ativo"),
        new("CLI-012", "Amanda Duarte", "amanda@agiletech.com", "(41) 94444-5000", "89.012.345/0001-67", "CNPJ", "AgileTech", "2024-07-01", "Ativo")
    ];

    private static readonly LicenseSeed[] LicenseSeeds =
    [
        new("XKWM-RTPL-BFQJ-YNVD", "TechCorp Brasil", "Myrati ERP", "Enterprise", 50, 42, "Ativa", "2025-01-15", "2026-01-15", 4500m, null, null),
        new("GHSE-LDNA-CWTF-KMPX", "InnovaDigital", "Myrati CRM", "Professional", 25, 18, "Ativa", "2025-03-01", "2026-03-01", 2200m, null, null),
        new("BJQR-WMVN-HXTP-DFLK", "SoluçõesTI", "Myrati ERP", "Starter", 10, 10, "Ativa", "2025-06-10", "2026-06-10", 890m, null, null),
        new("NVTC-KXRW-PLMS-FHJG", "DataFlow Ltda", "Myrati Analytics", "Completo", 100, 67, "Ativa", "2024-11-01", "2025-11-01", 3200m, 85000m, null),
        new("QLDF-BNWJ-XRKG-MHST", "NexGen Solutions", "Myrati CRM", "Starter", 5, 5, "Expirada", "2024-06-01", "2025-06-01", 450m, null, null),
        new("TWPH-FSMC-JXNL-VBKR", "CloudBase SA", "Myrati ERP", "Professional", 30, 22, "Ativa", "2025-02-15", "2026-02-15", 3100m, null, null),
        new("DKMR-GXLW-BNFS-QTJV", "GreenTech Inc", "Myrati Analytics", "Essencial", 15, 8, "Suspensa", "2025-04-01", "2026-04-01", 1500m, 42000m, null),
        new("FVJN-HRTK-WDXS-LCMP", "DigitalWave", "Myrati CRM", "Enterprise", 75, 61, "Ativa", "2025-01-01", "2026-01-01", 5500m, null, null),
        new("MXBL-PSKR-GNWT-JCHF", "FinanceHub", "Myrati ERP", "Enterprise", 200, 156, "Ativa", "2024-09-01", "2025-09-01", 12000m, null, null),
        new("RNWF-TDJQ-XHVK-BCMG", "StartupBox", "Myrati Analytics", "Essencial", 5, 3, "Pendente", "2026-03-01", "2027-03-01", 800m, 18000m, null),
        new("SHGK-VFXN-LDTW-PRJM", "MegaSoft", "Myrati CRM", "Professional", 40, 35, "Ativa", "2025-05-15", "2026-05-15", 3600m, null, null),
        new("JCNL-BWPF-MTXR-QVHK", "AgileTech", "Myrati HRM", "Padrão", 30, 0, "Pendente", "2026-04-01", "2027-04-01", 2000m, null, 5m)
    ];

    private static readonly ConnectedUserSeed[] ConnectedUserSeeds =
    [
        new("USR-001", "Carlos Silva", "carlos@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "Agora", "Online"),
        new("USR-005", "Marcos Ribeiro", "marcos@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "Agora", "Online"),
        new("USR-013", "Renata Gomes", "renata@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "10 min atrás", "Online"),
        new("USR-014", "Felipe Nascimento", "felipe@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "3 horas atrás", "Offline"),
        new("USR-015", "Daniela Moreira", "daniela@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "1 hora atrás", "Ausente"),
        new("USR-016", "Gustavo Pereira", "gustavo@techcorp.com.br", "TechCorp Brasil", "Myrati ERP", "Agora", "Online"),
        new("USR-002", "Ana Oliveira", "ana@innovadigital.com", "InnovaDigital", "Myrati CRM", "Agora", "Online"),
        new("USR-010", "Larissa Prado", "larissa@innovadigital.com", "InnovaDigital", "Myrati CRM", "1 hora atrás", "Offline"),
        new("USR-017", "Victor Hugo", "victor@innovadigital.com", "InnovaDigital", "Myrati CRM", "20 min atrás", "Ausente"),
        new("USR-003", "Roberto Santos", "roberto@solucoesti.com", "SoluçõesTI", "Myrati ERP", "5 min atrás", "Online"),
        new("USR-018", "Sandra Oliveira", "sandra@solucoesti.com", "SoluçõesTI", "Myrati ERP", "2 horas atrás", "Offline"),
        new("USR-004", "Fernanda Lima", "fernanda@dataflow.com", "DataFlow Ltda", "Myrati Analytics", "15 min atrás", "Ausente"),
        new("USR-009", "Thiago Nunes", "thiago@dataflow.com", "DataFlow Ltda", "Myrati Analytics", "Agora", "Online"),
        new("USR-019", "Isabela Rocha", "isabela@dataflow.com", "DataFlow Ltda", "Myrati Analytics", "Agora", "Online"),
        new("USR-020", "Ricardo Alves", "ricardo@dataflow.com", "DataFlow Ltda", "Myrati Analytics", "45 min atrás", "Ausente"),
        new("USR-021", "Pedro Almeida", "pedro@nexgen.com", "NexGen Solutions", "Myrati CRM", "5 dias atrás", "Offline"),
        new("USR-008", "Camila Torres", "camila@cloudbase.com", "CloudBase SA", "Myrati ERP", "Agora", "Online"),
        new("USR-022", "André Souza", "andre@cloudbase.com", "CloudBase SA", "Myrati ERP", "30 min atrás", "Ausente"),
        new("USR-023", "Marina Lopes", "marina@cloudbase.com", "CloudBase SA", "Myrati ERP", "Agora", "Online"),
        new("USR-024", "Lucas Ferreira", "lucas@greentech.com", "GreenTech Inc", "Myrati Analytics", "3 dias atrás", "Offline"),
        new("USR-006", "Patricia Vieira", "patricia@digitalwave.com", "DigitalWave", "Myrati CRM", "30 min atrás", "Ausente"),
        new("USR-025", "Juliana Mendes", "juliana@digitalwave.com", "DigitalWave", "Myrati CRM", "Agora", "Online"),
        new("USR-026", "Eduardo Martins", "eduardo@digitalwave.com", "DigitalWave", "Myrati CRM", "2 horas atrás", "Offline"),
        new("USR-027", "Carla Bezerra", "carla@digitalwave.com", "DigitalWave", "Myrati CRM", "Agora", "Online"),
        new("USR-007", "Diego Martins", "diego@financehub.com", "FinanceHub", "Myrati ERP", "2 horas atrás", "Offline"),
        new("USR-028", "Rafael Rodrigues", "rafael@financehub.com", "FinanceHub", "Myrati ERP", "Agora", "Online"),
        new("USR-029", "Tatiana Freitas", "tatiana@financehub.com", "FinanceHub", "Myrati ERP", "10 min atrás", "Online"),
        new("USR-030", "Beatriz Souza", "beatriz@startupbox.com", "StartupBox", "Myrati Analytics", "1 semana atrás", "Offline"),
        new("USR-011", "Bruno Cardoso", "bruno@megasoft.com", "MegaSoft", "Myrati CRM", "Agora", "Online"),
        new("USR-031", "Simone Dias", "simone@megasoft.com", "MegaSoft", "Myrati CRM", "Agora", "Online"),
        new("USR-032", "Leandro Campos", "leandro@megasoft.com", "MegaSoft", "Myrati CRM", "1 hora atrás", "Ausente"),
        new("USR-012", "Amanda Duarte", "amanda@agiletech.com", "AgileTech", "Myrati ERP", "10 min atrás", "Online"),
        new("USR-033", "Paulo Henrique", "paulo@agiletech.com", "AgileTech", "Myrati ERP", "Agora", "Online")
    ];

    private static readonly SprintSeed[] SprintSeeds =
    [
        new("SPR-001", "PRD-004", "Sprint 1 - Estrutura Base", "2026-01-05", "2026-01-19", "Concluída", 1),
        new("SPR-002", "PRD-004", "Sprint 2 - Módulo de Folha", "2026-01-19", "2026-02-02", "Concluída", 2),
        new("SPR-003", "PRD-004", "Sprint 3 - Ponto Eletrônico", "2026-02-02", "2026-02-16", "Ativa", 3),
        new("SPR-004", "PRD-004", "Sprint 4 - Recrutamento", "2026-02-16", "2026-03-02", "Planejada", 4),
        new("SPR-005", "PRD-004", "Sprint 5 - Testes & QA", "2026-03-02", "2026-03-16", "Planejada", 5)
    ];

    private static readonly TaskSeed[] TaskSeeds =
    [
        new("TSK-001", "PRD-004", "SPR-003", "Tela de registro de ponto", "Implementar a tela principal onde os colaboradores registram entrada, saída e intervalos.", "done", "high", "Maria Santos", ["frontend", "ui"], "2026-02-02", 1),
        new("TSK-002", "PRD-004", "SPR-003", "API de marcação de ponto", "Criar endpoints REST para POST/GET de registros de ponto com validações de horário.", "done", "high", "João Pereira", ["backend", "api"], "2026-02-02", 2),
        new("TSK-003", "PRD-004", "SPR-003", "Relatório de horas trabalhadas", "Dashboard com total de horas, horas extras e banco de horas por colaborador no período.", "in_progress", "medium", "Maria Santos", ["frontend", "relatorio"], "2026-02-03", 1),
        new("TSK-004", "PRD-004", "SPR-003", "Integração com biometria", "Integrar SDK de leitores biométricos para registro automático de ponto.", "in_progress", "critical", "João Pereira", ["backend", "integracao"], "2026-02-04", 2),
        new("TSK-005", "PRD-004", "SPR-003", "Notificações de atraso", "Sistema de alertas automáticos quando o colaborador não registra ponto no horário previsto.", "todo", "medium", "Ana Costa", ["backend", "notificacao"], "2026-02-05", 1),
        new("TSK-006", "PRD-004", "SPR-003", "Aprovação de horas extras", "Fluxo de aprovação para gestores validarem horas extras antes do fechamento.", "todo", "high", "Maria Santos", ["frontend", "workflow"], "2026-02-06", 2),
        new("TSK-007", "PRD-004", "SPR-003", "Testes unitários - Ponto", "Escrever testes unitários para toda a lógica de cálculo de horas e validações.", "backlog", "low", "João Pereira", ["teste", "qualidade"], "2026-02-07", 1),
        new("TSK-008", "PRD-004", "SPR-003", "Exportação de espelho de ponto", "Gerar PDF/Excel do espelho de ponto mensal conforme legislação trabalhista.", "review", "medium", "Ana Costa", ["backend", "relatorio"], "2026-02-03", 1),
        new("TSK-009", "PRD-004", "SPR-004", "Cadastro de vagas", "CRUD completo de vagas com campos de cargo, departamento, requisitos e faixa salarial.", "backlog", "high", string.Empty, ["frontend", "backend"], "2026-02-10", 1),
        new("TSK-010", "PRD-004", "SPR-004", "Pipeline de candidatos", "Kanban visual para movimentação de candidatos nas etapas do processo seletivo.", "backlog", "high", string.Empty, ["frontend", "ui"], "2026-02-10", 2),
        new("TSK-011", "PRD-004", "SPR-004", "Portal do candidato", "Página pública para candidatos se inscreverem e acompanharem o status da candidatura.", "backlog", "medium", string.Empty, ["frontend", "publico"], "2026-02-10", 3),
        new("TSK-012", "PRD-004", "SPR-005", "Testes E2E - Fluxo completo", "Suite completa de testes end-to-end cobrindo os fluxos principais do HRM.", "backlog", "critical", string.Empty, ["teste", "qualidade"], "2026-02-14", 1),
        new("TSK-013", "PRD-004", "SPR-005", "Testes de performance", "Stress test e benchmark de carga com simulação de 500+ usuários simultâneos.", "backlog", "high", string.Empty, ["teste", "performance"], "2026-02-14", 2)
    ];

    private static readonly RevenueSnapshotSeed[] RevenueSnapshotSeeds =
    [
        new("REV-001", "Mar", 32400m, 45, 1),
        new("REV-002", "Abr", 34100m, 48, 2),
        new("REV-003", "Mai", 35800m, 50, 3),
        new("REV-004", "Jun", 33900m, 47, 4),
        new("REV-005", "Jul", 37200m, 52, 5),
        new("REV-006", "Ago", 38500m, 55, 6),
        new("REV-007", "Set", 39100m, 56, 7),
        new("REV-008", "Out", 40300m, 58, 8),
        new("REV-009", "Nov", 41800m, 60, 9),
        new("REV-010", "Dez", 42500m, 62, 10),
        new("REV-011", "Jan", 43200m, 64, 11),
        new("REV-012", "Fev", 44950m, 67, 12)
    ];

    private static readonly DashboardActivitySeed[] DashboardActivitySeeds =
    [
        new("DA-001", "Nova licença criada", "StartupBox - Myrati Analytics (Starter)", "Há 2 horas", "create", 1),
        new("DA-002", "Licença renovada", "TechCorp Brasil - Myrati ERP (Enterprise)", "Há 5 horas", "renew", 2),
        new("DA-003", "Licença suspensa", "GreenTech Inc - Myrati Analytics (Professional)", "Há 1 dia", "suspend", 3),
        new("DA-004", "Novo cliente cadastrado", "StartupBox - Beatriz Souza", "Há 2 dias", "client", 4),
        new("DA-005", "Licença expirada", "NexGen Solutions - Myrati CRM (Starter)", "Há 3 dias", "expire", 5),
        new("DA-006", "Upgrade de plano", "DigitalWave - Professional > Enterprise", "Há 4 dias", "upgrade", 6)
    ];

    private static readonly ApiKeySeed[] ApiKeySeeds =
    [
        new("AK-001", "Produção", "myra_prod_", "e7f2a1b9c4d3e8f5", true, "2025-01-10"),
        new("AK-002", "Staging", "myra_stg_", "b3c5d8e1f4a7b2c6", true, "2025-03-05")
    ];

    private static readonly AdminUserSeed[] AdminUserSeeds =
    [
        new("TM-001", "Admin Master", "admin@myrati.com", "(11) 99876-5432", "Super Admin", "Ativo", "Tecnologia", "São Paulo, SP", true),
        new("TM-002", "Maria Santos", "maria@myrati.com", "(11) 97777-1000", "Admin", "Ativo", "Operações", "São Paulo, SP", false),
        new("TM-003", "João Pereira", "joao@myrati.com", "(21) 98888-2000", "Desenvolvedor", "Ativo", "Plataforma", "Rio de Janeiro, RJ", false),
        new("TM-004", "Ana Costa", "ana@myrati.com", "(31) 96666-3000", "Admin", "Convite Pendente", "Comercial", "Belo Horizonte, MG", false),
        new("TM-005", "Bruno Lima", "bruno@myrati.com", "(41) 95555-4000", "Desenvolvedor", "Ativo", "Produto", "Curitiba, PR", false)
    ];

    private static readonly ProductCollaboratorSeed[] ProductCollaboratorSeeds =
    [
        new("PRD-001", "TM-003", "2025-06-10", true, true, true, true, true, true, true, false, true, false, false, false, true, false, false, false),
        new("PRD-001", "TM-005", "2025-08-20", true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, false),
        new("PRD-004", "TM-003", "2026-02-10", true, true, true, true, true, true, true, false, true, false, false, false, true, false, false, false),
        new("PRD-004", "TM-005", "2026-02-12", true, true, true, false, true, false, false, false, true, false, false, false, true, false, false, false)
    ];

    private static readonly ProfileSessionSeed[] ProfileSessionSeeds =
    [
        new("1", "TM-001", "São Paulo, SP", "Agora (sessão atual)", true),
        new("2", "TM-001", "São Paulo, SP", "Há 2 horas", false),
        new("3", "TM-001", "Rio de Janeiro, RJ", "Há 1 dia", false)
    ];

    private static readonly ProfileActivitySeed[] ProfileActivitySeeds =
    [
        new("ACT-001", "TM-001", "Login realizado", "09/03/2026 08:32"),
        new("ACT-002", "TM-001", "Senha alterada", "05/03/2026 14:15"),
        new("ACT-003", "TM-001", "Perfil atualizado", "01/03/2026 10:42"),
        new("ACT-004", "TM-001", "2FA ativado", "25/02/2026 16:20"),
        new("ACT-005", "TM-001", "Login realizado", "25/02/2026 09:10")
    ];

    private static readonly SystemComponentSeed[] SystemComponentSeeds =
    [
        new("STS-001", "API Principal", "operational", "99.98%", "45ms", 1),
        new("STS-002", "Painel Administrativo", "operational", "99.99%", "120ms", 2),
        new("STS-003", "Banco de Dados", "operational", "99.99%", "12ms", 3),
        new("STS-004", "Autenticação (OAuth)", "operational", "99.97%", "65ms", 4),
        new("STS-005", "Webhooks", "operational", "99.95%", "89ms", 5),
        new("STS-006", "CDN & Assets", "operational", "100%", "18ms", 6)
    ];

    private static readonly SystemIncidentSeed[] SystemIncidentSeeds =
    [
        new("INC-001", "05 Mar 2026", "Manutenção programada — Banco de Dados", "Migração de banco concluída com sucesso. Tempo de inatividade: 12 minutos.", true, 1),
        new("INC-002", "28 Fev 2026", "Latência elevada na API Principal", "Identificado pico de tráfego atípico. Auto-scaling ativado, normalizado em 8 minutos.", true, 2),
        new("INC-003", "15 Fev 2026", "Atraso na entrega de webhooks", "Fila de webhooks represada por 23 minutos. Causa: atualização de dependência. Corrigido.", true, 3)
    ];

    private static readonly UptimeSampleSeed[] UptimeSampleSeeds =
    [
        new("UPT-001", "Seg", 100m, 1),
        new("UPT-002", "Ter", 100m, 2),
        new("UPT-003", "Qua", 99.9m, 3),
        new("UPT-004", "Qui", 100m, 4),
        new("UPT-005", "Sex", 100m, 5),
        new("UPT-006", "Sáb", 100m, 6),
        new("UPT-007", "Dom", 100m, 7),
        new("UPT-008", "Seg", 100m, 8),
        new("UPT-009", "Ter", 99.8m, 9),
        new("UPT-010", "Qua", 100m, 10),
        new("UPT-011", "Qui", 100m, 11),
        new("UPT-012", "Sex", 100m, 12),
        new("UPT-013", "Sáb", 100m, 13),
        new("UPT-014", "Dom", 100m, 14)
    ];

    private sealed record ProductSeed(
        string Id,
        string Name,
        string Description,
        string Category,
        string Status,
        string SalesStrategy,
        string CreatedDate,
        string Version,
        IReadOnlyCollection<PlanSeed> Plans);

    private sealed record PlanSeed(
        string Name,
        int MaxUsers,
        decimal MonthlyPrice,
        decimal? DevelopmentCost,
        decimal? MaintenanceCost,
        decimal? RevenueSharePercent);

    private sealed record ClientSeed(
        string Id,
        string Name,
        string Email,
        string Phone,
        string Document,
        string DocumentType,
        string Company,
        string JoinedDate,
        string Status);

    private sealed record LicenseSeed(
        string Id,
        string ClientCompany,
        string ProductName,
        string Plan,
        int MaxUsers,
        int ActiveUsers,
        string Status,
        string StartDate,
        string ExpiryDate,
        decimal MonthlyValue,
        decimal? DevelopmentCost,
        decimal? RevenueSharePercent);

    private sealed record ConnectedUserSeed(
        string Id,
        string Name,
        string Email,
        string ClientCompany,
        string ProductName,
        string LastActive,
        string Status);

    private sealed record SprintSeed(
        string Id,
        string ProductId,
        string Name,
        string StartDate,
        string EndDate,
        string Status,
        int SortOrder);

    private sealed record TaskSeed(
        string Id,
        string ProductId,
        string SprintId,
        string Title,
        string Description,
        string Column,
        string Priority,
        string Assignee,
        IReadOnlyCollection<string> Tags,
        string CreatedDate,
        int SortOrder);

    private sealed record RevenueSnapshotSeed(string Id, string Month, decimal Revenue, int Licenses, int SortOrder);

    private sealed record DashboardActivitySeed(
        string Id,
        string Action,
        string Description,
        string Time,
        string Type,
        int SortOrder);

    private sealed record ApiKeySeed(string Id, string Label, string Prefix, string Secret, bool Active, string CreatedAt);

    private sealed record AdminUserSeed(
        string Id,
        string Name,
        string Email,
        string Phone,
        string Role,
        string Status,
        string Department,
        string Location,
        bool IsPrimaryAccount);

    private sealed record ProductCollaboratorSeed(
        string ProductId,
        string MemberId,
        string AddedDate,
        bool TasksView,
        bool TasksCreate,
        bool TasksEdit,
        bool TasksDelete,
        bool SprintsView,
        bool SprintsCreate,
        bool SprintsEdit,
        bool SprintsDelete,
        bool LicensesView,
        bool LicensesCreate,
        bool LicensesEdit,
        bool LicensesDelete,
        bool ProductView,
        bool ProductCreate,
        bool ProductEdit,
        bool ProductDelete);

    private sealed record ProfileSessionSeed(string Id, string AdminUserId, string Location, string LastActive, bool IsCurrent);

    private sealed record ProfileActivitySeed(string Id, string AdminUserId, string Action, string Date);

    private sealed record SystemComponentSeed(
        string Id,
        string Name,
        string Status,
        string Uptime,
        string ResponseTime,
        int SortOrder);

    private sealed record SystemIncidentSeed(
        string Id,
        string Date,
        string Title,
        string Description,
        bool Resolved,
        int SortOrder);

    private sealed record UptimeSampleSeed(string Id, string Day, decimal Pct, int SortOrder);
}
