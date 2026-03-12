using Myrati.API.Controllers;
using Myrati.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddMyratiServiceHost(
    "Myrati Notification Service",
    typeof(NotificationsController));

var app = await builder.BuildMyratiServiceAsync();

app.Run();

public partial class Program;
