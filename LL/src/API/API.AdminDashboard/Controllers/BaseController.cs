using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.AdminDashboard.Controllers;

/// <summary>
/// Base controller
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[ApiConventionType(typeof(DefaultApiConventions))]
public abstract class BaseController : ControllerBase
{
    private ISender _mediator = null!;

    /// <summary>
    /// Mediator injection
    /// </summary>
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetService<ISender>() ?? throw new SystemException(nameof(_mediator));
}