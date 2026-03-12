using Myrati.API.Controllers;
using Myrati.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddMyratiServiceHost(
    "Myrati Identity Service",
    typeof(AuthController),
    typeof(ProfileController),
    typeof(SettingsController));

var app = await builder.BuildMyratiServiceAsync();

app.Run();

public partial class Program;
