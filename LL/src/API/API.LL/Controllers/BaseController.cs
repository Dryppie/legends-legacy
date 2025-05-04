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

    /// <summary>
    /// Mediator injection
    /// </summary>
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetService<ISender>() ?? throw new SystemException(nameof(_mediator));

    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.UserData)!);
    protected Guid CurrentCharacterGuid => Guid.Parse(User.FindFirstValue("CharacterId")!);

    protected bool IsLocal()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}