using Moq;
using Myrati.Application.Abstractions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Domain.Clients;
using Myrati.Domain.Costs;
using Myrati.Domain.Identity;
using Myrati.Domain.Products;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsGlobalDashboardForAdmin()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-001");
        userContext.Setup(x => x.Email).Returns("admin@myrati.com");
        userContext.Setup(x => x.Role).Returns("Admin");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.True(result.CanViewRevenue);
        Assert.True(result.TotalLicensesCount > 0);
        Assert.NotEmpty(result.ProductHealth);
    }

    [Fact]
    public async Task GetAsync_ReturnsDeveloperDashboardForDeveloper()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-003");
        userContext.Setup(x => x.Email).Returns("joao@myrati.com");
        userContext.Setup(x => x.Role).Returns("Desenvolvedor");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAsync_ForDeveloper_OnlyCountsPlanVisibleProductsInAvailableBalance()
    {
        await using var scope = await SeededDbContextScope.CreateAsync(seed: false);
        var today = Myrati.Application.Common.ApplicationTime.LocalToday();

        scope.Context.AddRange(
            new AdminUser
            {
                Id = "TM-100",
                Name = "Dev Financeiro",
                Email = "dev.financeiro@myrati.com",
                Role = "Desenvolvedor",
                Status = "Ativo"
            },
            new Client
            {
                Id = "CLI-100",
                Name = "Cliente Financeiro",
                Email = "cliente@myrati.com",
                Phone = "(11) 99999-9999",
                Document = "12.345.678/0001-90",
                DocumentType = "CNPJ",
                Company = "Cliente Financeiro Ltda",
                JoinedDate = today,
                Status = "Ativo"
            },
            new Product
            {
                Id = "PRD-VISIBLE",
                Name = "Produto B",
                Description = "Produto visivel",
                Category = "Financeiro",
                Status = "Ativo",
                SalesStrategy = "subscription",
                CreatedDate = today
            },
            new Product
            {
                Id = "PRD-HIDDEN",
                Name = "Produto C",
                Description = "Produto sem acesso comercial",
                Category = "Financeiro",
                Status = "Ativo",
                SalesStrategy = "subscription",
                CreatedDate = today
            },
            new ProductPlan
            {
                Id = "PLN-VISIBLE",
                ProductId = "PRD-VISIBLE",
                Name = "Base",
                MaxUsers = null,
                MonthlyPrice = 30m
            },
            new ProductPlan
            {
                Id = "PLN-HIDDEN",
                ProductId = "PRD-HIDDEN",
                Name = "Base",
                MaxUsers = null,
                MonthlyPrice = 50m
            },
            new License
            {
                Id = "LIC-VISIBLE",
                ClientId = "CLI-100",
                ProductId = "PRD-VISIBLE",
                Plan = "Base",
                MaxUsers = null,
                ActiveUsers = 0,
                Status = "Ativa",
                StartDate = today.AddDays(-14),
                ExpiryDate = today.AddYears(1),
                MonthlyValue = 30m
            },
            new License
            {
                Id = "LIC-HIDDEN",
                ClientId = "CLI-100",
                ProductId = "PRD-HIDDEN",
                Plan = "Base",
                MaxUsers = null,
                ActiveUsers = 0,
                Status = "Ativa",
                StartDate = today.AddDays(-14),
                ExpiryDate = today.AddYears(1),
                MonthlyValue = 50m
            },
            new ProductExpense
            {
                Id = "EXP-VISIBLE",
                ProductId = "PRD-VISIBLE",
                Name = "Infra visivel",
                Category = "hosting",
                Amount = 20m,
                Recurrence = "monthly",
                CreatedDate = today
            },
            new ProductExpense
            {
                Id = "EXP-HIDDEN",
                ProductId = "PRD-HIDDEN",
                Name = "Infra oculta",
                Category = "hosting",
                Amount = 15m,
                Recurrence = "monthly",
                CreatedDate = today
            },
            new CashTransaction
            {
                Id = "TXN-VISIBLE",
                Type = "deposit",
                Category = "development_payment",
                Amount = 5000m,
                Description = "Pagamento extra visível",
                ReferenceProductId = "PRD-VISIBLE",
                ReferenceProductName = "Produto B",
                Date = today,
                CreatedAtUtc = DateTimeOffset.UtcNow
            },
            new CashTransaction
            {
                Id = "TXN-HIDDEN",
                Type = "deposit",
                Category = "development_payment",
                Amount = 2500m,
                Description = "Pagamento extra oculto",
                ReferenceProductId = "PRD-HIDDEN",
                ReferenceProductName = "Produto C",
                Date = today,
                CreatedAtUtc = DateTimeOffset.UtcNow
            },
            new ProductCollaborator
            {
                ProductId = "PRD-VISIBLE",
                MemberId = "TM-100",
                AddedDate = today,
                TasksView = true,
                PlansView = true,
                ProductView = true
            },
            new ProductCollaborator
            {
                ProductId = "PRD-HIDDEN",
                MemberId = "TM-100",
                AddedDate = today,
                TasksView = true,
                PlansView = false,
                ProductView = true
            });

        await scope.Context.SaveChangesAsync();

        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-100");
        userContext.Setup(x => x.Email).Returns("dev.financeiro@myrati.com");
        userContext.Setup(x => x.Role).Returns("Desenvolvedor");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.True(result.CanViewRevenue);
        Assert.Equal(5010m, result.AvailableBalance);
        Assert.Equal(5030m, result.TotalMonthlyRevenue);
        Assert.Equal(20m, result.TotalMonthlyProductExpenses);
        var visibleProduct = Assert.Single(result.RevenueByProduct);
        Assert.Equal("Produto B", visibleProduct.Name);
        Assert.Equal(5030m, visibleProduct.Value);
        Assert.Contains(result.ProductHealth, item => item.ProductId == "PRD-HIDDEN" && item.Revenue == 0m);
    }

    [Fact]
    public async Task GetAsync_ForAdmin_UsesFixedMaintenancePlusExpenseMarginForDevelopmentRevenue()
    {
        await using var scope = await SeededDbContextScope.CreateAsync(seed: false);
        var today = Myrati.Application.Common.ApplicationTime.LocalToday();

        scope.Context.AddRange(
            new AdminUser
            {
                Id = "TM-200",
                Name = "Admin Financeiro",
                Email = "admin.financeiro@myrati.com",
                Role = "Admin",
                Status = "Ativo"
            },
            new Product
            {
                Id = "PRD-DEV",
                Name = "Produto Manutencao",
                Description = "Produto para validar manutencao fixa + margem.",
                Category = "Financeiro",
                Status = "Ativo",
                SalesStrategy = "development",
                CreatedDate = today
            },
            new ProductPlan
            {
                Id = "PLN-DEV",
                ProductId = "PRD-DEV",
                Name = "Base",
                MaxUsers = null,
                MonthlyPrice = 0m,
                DevelopmentCost = 1000m,
                MaintenanceCost = 100m,
                MaintenanceProfitMargin = 30m
            },
            new ProductExpense
            {
                Id = "EXP-DEV-MONTHLY",
                ProductId = "PRD-DEV",
                Name = "Hospedagem",
                Category = "hosting",
                Amount = 70m,
                Recurrence = "monthly",
                CreatedDate = today
            },
            new ProductExpense
            {
                Id = "EXP-DEV-ANNUAL",
                ProductId = "PRD-DEV",
                Name = "Dominio",
                Category = "domain",
                Amount = 80m,
                Recurrence = "annual",
                CreatedDate = today
            });

        await scope.Context.SaveChangesAsync();

        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-200");
        userContext.Setup(x => x.Email).Returns("admin.financeiro@myrati.com");
        userContext.Setup(x => x.Role).Returns("Admin");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.Equal(123m, result.TotalMonthlyRevenue);
        var productRevenue = Assert.Single(result.RevenueByProduct);
        Assert.Equal("Produto Manutencao", productRevenue.Name);
        Assert.Equal(123m, productRevenue.Value);
        Assert.Contains(result.ProductHealth, item => item.ProductId == "PRD-DEV" && item.Revenue == 123m);
    }
}
