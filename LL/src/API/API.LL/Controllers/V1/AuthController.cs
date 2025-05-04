using System.Security.Claims;
using API.LL.Controllers;
using Application.Common.Responses;
using Application.UseCases.Authorization.Commands.CreateNewTokens;
using Application.UseCases.Authorization.Queries.ValidateToken;
using Application.UseCases.Users.Commands.BindGoogle;
using Application.UseCases.Users.Commands.ConvertGuestToUser;
using Application.UseCases.Users.Commands.GoogleLogin;
using Application.UseCases.Users.Commands.GuestLogin;
using Application.UseCases.Users.Commands.Register;
using Application.UseCases.Users.Dtos;
using Application.UseCases.Users.Queries.GetUserInfo;
using Application.UseCases.Users.Queries.Login;
using Common.Authorization.Security;
using Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class AuthController : BaseController
{
    public record UserLoginDto(string Email, string Password);
    public record UserRegisterDto(string Username, string Email, string Password);

    private const string AccessTokenCookie = "AccessToken";
    private const string RefreshTokenCookie = "RefreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Unit>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<Unit>>> Register([FromBody] UserRegisterDto input)
    {
        var response = await Mediator.Send(new RegisterCommand(input.Username, input.Email, input.Password));

        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<Tokens>>> Login([FromBody] UserLoginDto input)
    {
        var result = await Mediator.Send(new LoginQuery(input.Email, input.Password));
        if (result.Data is null) return BadRequest(result);

        SetAuthCookies(result.Data);
        return Ok(result);
    }

    [HttpPost("loginAsGuest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Tokens>> LoginAsGuest()
    {
        var result = await Mediator.Send(new GuestLoginCommand());

        if (result.Data is null) return BadRequest(result);

        SetAuthCookies(result.Data);
        return Ok(result);
    }

    [HttpPost("convertGuestToUser")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ConvertGuestToUser([FromBody] UserRegisterDto input)
    {
        var succeeded = await Mediator.Send(new ConvertGuestToUserCommand(CurrentUserId.ToString(), input.Username, input.Email, input.Password));

        return succeeded
             ? Ok(new { isValid = true, message = "Account successfully converted" })
             : BadRequest(new { isValid = false, message = "Failed to convert guest to user" });
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<Tokens>>> GoogleLogin([FromBody] string idToken)
    {
        var res = await Mediator.Send(new GoogleLoginCommand(idToken));
        if (res.Data is null) return BadRequest(res);
        SetAuthCookies(res.Data);
        return Ok(res);
    }

    [HttpPost("bind-google")]
    [Authorize]
    public async Task<IActionResult> BindGoogle([FromBody] string idToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.UserData)!);
        var res = await Mediator.Send(new BindGoogleCommand(userId, idToken));
        return res.IsSuccess ? Ok(res) : BadRequest(res);
    }

    /// <summary>
    /// Logs out a user from Web
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Logout()
    {
        var opts = BuildCookieOptions();
        Response.Cookies.Delete(AccessTokenCookie, opts);
        Response.Cookies.Delete(RefreshTokenCookie, opts);
        return Ok();
    }

    [HttpPost("createNewTokens")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Response<Tokens>>> CreateNewTokens()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrWhiteSpace(refreshToken)) return BadRequest();

        try
        {
            var result = await Mediator.Send(new CreateNewTokensCommand(refreshToken));

            if (result.Data is null) return BadRequest(result);

            SetAuthCookies(result.Data);
            return Ok(result);
        }
        catch (InvalidRefreshTokenException)
        {
            Response.Headers.Append("invalid_refresh_token",
                "The refresh token is expired, revoked, malformed, or otherwise invalid.");
            return Unauthorized();
        }
    }

    /// <summary>
    /// Validates a jwt access token
    /// </summary>
    [HttpPost("validateToken")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<bool> ValidateToken([FromBody] string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest();

        var result = Mediator.Send(new ValidateTokenQuery(token));

        return result.Result ? Ok() : Unauthorized();

    }

    [HttpGet("getUserInfo")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUserInfo()
    {
        return await Mediator.Send(new GetUserInfoQuery(CurrentUserId));
    }

    private void SetAuthCookies(Tokens t)
    {
        var opts = BuildCookieOptions();
        Response.Cookies.Append(AccessTokenCookie, t.AccessToken, opts);
        Response.Cookies.Append(RefreshTokenCookie, t.RefreshToken, opts);
    }

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        IsEssential = true,
        Path = "/",
        SameSite = /*_env.IsDevelopment() ?*/ SameSiteMode.None /*: SameSiteMode.Strict*/,
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        MaxAge = TimeSpan.FromDays(365)
        // Domain   = _cfg["HostedDomain"] // ← uncomment if you serve from a sub‑domain
    };
}