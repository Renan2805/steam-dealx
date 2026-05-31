using SteamDealX.Api.Errors;
using Microsoft.AspNetCore.Mvc;

namespace SteamDealX.Api.Controllers;

/// <summary>Controller base com helper de resposta de erro padronizada (RFC 9457).</summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ApiProblem(int status, string code, string title, string detail) =>
        new ObjectResult(new ApiError
        {
            Type     = $"https://tools.ietf.org/html/rfc9110#section-15.{status / 100}.{status % 100 / 10}",
            Status   = status,
            Title    = title,
            Detail   = detail,
            Instance = Request.Path,
            Code     = code
        })
        {
            StatusCode   = status,
            ContentTypes = { "application/problem+json" }
        };
}
