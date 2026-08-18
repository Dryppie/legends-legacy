using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.LL.Controllers;

/// <summary>
/// Base controller
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ApiConventionType(typeof(DefaultApiConventions))]
public abstract class BaseController : ControllerBase
{
    private ISender _mediator = null!;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetService<ISender>() ?? throw new SystemException(nameof(_mediator));

    protected Guid CurrentUserId => GetGuidClaim(ClaimTypes.UserData, "User ID");
    protected Guid CurrentCharacterGuid => GetGuidClaim("CharacterId", "Character ID");

    protected bool IsLocal()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    private Guid GetGuidClaim(string claimType, string name)
    {
        var value = User.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException($"{name} claim is missing.");
        }
        if (!Guid.TryParse(value, out var guid))
            throw new UnauthorizedAccessException($"{name} claim is invalid.");

        return guid;
    }
}
