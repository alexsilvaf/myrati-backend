using Myrati.API.Controllers;
using Myrati.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddMyratiServiceHost(
    "Myrati Public Service",
    typeof(PublicController),
    typeof(LicenseActivationController));

var app = await builder.BuildMyratiServiceAsync();

app.Run();

public partial class Program;
