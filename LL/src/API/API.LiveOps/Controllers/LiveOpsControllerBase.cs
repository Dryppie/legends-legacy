using System.Security.Claims;
using Domain.Models.Administration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LiveOps.Controllers;

[Authorize]
[ApiController]
[Produces("application/json")]
public abstract class LiveOpsControllerBase : ControllerBase
{
    private ISender _mediator = null!;

    protected ISender Mediator => _mediator ??=
        HttpContext.RequestServices.GetRequiredService<ISender>();

    protected AdministrationActor CurrentActor
    {
        get
        {
            var subject = User.FindFirstValue("sub")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("oid");
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new UnauthorizedAccessException(
                    "The staff access token is missing a stable subject claim.");
            }

            var displayName = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("preferred_username")
                ?? User.Identity?.Name
                ?? subject;
            return new AdministrationActor(subject, displayName);
        }
    }
}
