using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Myrati.API.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected string GetCurrentUserEmail() =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? throw new UnauthorizedAccessException("Usuário autenticado sem e-mail no token.");
}
