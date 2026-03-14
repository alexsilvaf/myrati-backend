using Myrati.API.Controllers;
using Myrati.Application.Services;
using Myrati.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddMyratiServiceHost(
    "Myrati Public Service",
    typeof(PublicController),
    typeof(LicenseActivationController));
builder.Services.AddHttpClient<SystemStatusMonitorRunner>();
builder.Services.AddHostedService<SystemStatusMonitorBackgroundService>();

var app = await builder.BuildMyratiServiceAsync();

app.Run();

public partial class Program;
