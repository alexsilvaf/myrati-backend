using Myrati.API.Controllers;
using Myrati.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddMyratiServiceHost(
    "Myrati Backoffice Service",
    typeof(AuditLogsController),
    typeof(ComplianceController),
    typeof(CostsController),
    typeof(DashboardController),
    typeof(TransactionsController),
    typeof(ProductsController),
    typeof(ProductKanbanController),
    typeof(LicensesController),
    typeof(ClientsController),
    typeof(UsersController),
    typeof(PortalController),
    typeof(StreamController));

var app = await builder.BuildMyratiServiceAsync(initializeDatabase: true);

app.Run();

public partial class Program;
