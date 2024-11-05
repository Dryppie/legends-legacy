using API.LL.Controllers;
using Application.UseCases.Authorization.Commands.CreateNewTokens;
using Application.UseCases.Authorization.Queries.ValidateToken;
using Application.UseCases.Users.Commands.ConvertGuestToUser;
using Application.UseCases.Users.Commands.GuestLogin;
using Application.UseCases.Users.Commands.Register;
using Application.UseCases.Users.Queries.Login;
using Common.Authorization.Security;
using Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class AuthController : BaseController
{
    public record UserLoginDto(string Email, string Password);
    public record UserRegisterDto(string Username, string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Tokens>> Login([FromBody] UserLoginDto input)
    {
        var tokens = await Mediator.Send(new LoginQuery(input.Email, input.Password));
        var cookies = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("AccessToken", tokens.AccessToken),
            new KeyValuePair<string, string>("RefreshToken", tokens.RefreshToken)
        };

        Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

        return Ok(cookies);
    }

    [AllowAnonymous]
    [HttpPost("loginAsGuest")]
    public async Task<ActionResult<Tokens>> LoginAsGuest()
    {
        var tokens = await Mediator.Send(new GuestLoginCommand());
        var cookies = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("AccessToken", tokens.AccessToken),
            new KeyValuePair<string, string>("RefreshToken", tokens.RefreshToken)
        };

        Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

        return Ok(cookies);
    }

    [Authorize] // User must be authenticated
    [HttpPost("convertGuestToUser")]
    public async Task<ActionResult> ConvertGuestToUser([FromBody] UserRegisterDto input)
    {
        var userId = User.FindFirst(ClaimTypes.UserData)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await Mediator.Send(new ConvertGuestToUserCommand(userId, input.Username, input.Email, input.Password));

        if (result)
            return Ok();
        else
            return BadRequest();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] UserRegisterDto input)
    {
        await Mediator.Send(new RegisterCommand(input.Username, input.Email, input.Password));

        return Ok();
    }

    /// <summary>
    /// Logs out a user from Web
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Logout()
    {
        var cookieOptions = GetCookieOptions();
        Response.Cookies.Delete("AccessToken", cookieOptions);
        Response.Cookies.Delete("RefreshToken", cookieOptions);

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("createNewTokens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<Tokens> CreateNewTokens()
    {
        var refreshToken = HttpContext.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest();

        try
        {
            var result = Mediator.Send(new CreateNewTokensCommand(refreshToken));
            var tokens = result.Result;

            var cookies = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("AccessToken", tokens.AccessToken),
                new KeyValuePair<string, string>("RefreshToken", tokens.RefreshToken)
            };

            Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

            return Ok(cookies);
        }
        catch (InvalidRefreshTokenException)
        {
            HttpContext.Response.Headers.Append("invalid_refresh_token", "The refresh token provided is expired, revoked, malformed, or invalid for other reasons");
            return Unauthorized();
        }

    }

    /// <summary>
    /// Validates a jwt access token
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validateToken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<bool> ValidateToken([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest();
        }

        var result = Mediator.Send(new ValidateTokenQuery(token));

        if (!result.Result)
        {
            return Unauthorized();
        }

        return Ok();
    }

    private CookieOptions GetCookieOptions() => new()
    {
        Secure = true,
        HttpOnly = true,
        Path = "/",
        //Domain = "hi", //_configuration["HostedDomain"], // the top level domain such as bupl.dk or webtestbupl.dk
        SameSite = IsLocal() ? SameSiteMode.None : SameSiteMode.Strict,
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        MaxAge = TimeSpan.FromDays(365),
    };
}