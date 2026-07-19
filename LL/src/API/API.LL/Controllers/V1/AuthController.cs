using API.LL.Controllers;
using Application.UseCases.Authorization.Commands.CreateNewTokens;
using API.LL.Common;
using Application.UseCases.Authorization.Commands.Logout;
using Application.UseCases.Authorization.Queries.ValidateToken;
using Application.UseCases.Users.Commands.BindGoogle;
using Application.UseCases.Users.Commands.ConvertGuestToUser;
using Application.UseCases.Users.Commands.GoogleLogin;
using Application.UseCases.Users.Commands.GuestLogin;
using Application.UseCases.Users.Commands.Login;
using Application.UseCases.Users.Commands.Register;
using Application.UseCases.Users.Commands.RenameCharacter;
using Application.UseCases.Users.Dtos;
using Application.UseCases.Users.Queries.GetUserInfo;
using Common.Authorization.Security;
using Common.Exceptions;
using Common.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class AuthController : BaseController
{
    public record UserLoginDto(string Email, string Password);
    public sealed record UserRegisterDto
    {
        public string? CharacterName { get; init; }
        public string? Username { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;

        public string RequestedCharacterName => CharacterName ?? Username ?? string.Empty;
    }

    private const string AccessTokenCookie = "AccessToken";
    private const string RefreshTokenCookie = "RefreshToken";
    private const string RefreshCookieCsrfHeader = "X-LL-Refresh-Request";
    private readonly IWebHostEnvironment _env;
    private readonly RefreshTokenRotationCoordinator _refreshTokenRotation;

    public AuthController(
        IWebHostEnvironment env,
        RefreshTokenRotationCoordinator refreshTokenRotation)
    {
        _env = env;
        _refreshTokenRotation = refreshTokenRotation;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<Tokens>>> Register([FromBody] UserRegisterDto input)
    {
        var result = await Mediator.Send(new RegisterCommand(input.RequestedCharacterName, input.Email, input.Password));
        if (result.Data is null) return BadRequest(result);

        SetAuthCookies(result.Data);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<Tokens>>> Login([FromBody] UserLoginDto input)
    {
        var result = await Mediator.Send(new LoginCommand(input.Email, input.Password));
        if (result.Data is null) return BadRequest(result);

        SetAuthCookies(result.Data);
        return Ok(result);
    }

    [HttpPost("loginAsGuest")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<Tokens>>> LoginAsGuest()
    {
        var result = await Mediator.Send(new GuestLoginCommand());
        if (result.Data is null) return result;

        SetAuthCookies(result.Data);
        return result;
    }

    [HttpPost("convertGuestToUser")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Response<Tokens>>> ConvertGuestToUser([FromBody] UserRegisterDto input)
    {
        var result = await Mediator.Send(new ConvertGuestToUserCommand(CurrentUserId, input.RequestedCharacterName, input.Email, input.Password));
        if (result.Data is null) return result;

        SetAuthCookies(result.Data);
        return result;
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
    public async Task<ActionResult<Response<Tokens>>> BindGoogle([FromBody] string idToken)
    {
        var result = await Mediator.Send(new BindGoogleCommand(CurrentUserId, idToken));
        if (result.Data is null) return result;

        SetAuthCookies(result.Data);
        return result;
    }

    /// <summary>
    /// Logs out a user from Web
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout()
    {
        if (!HasRefreshCookieCsrfHeader()) return BadRequest();

        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var refresh))
        {
            await Mediator.Send(new LogoutCommand(refresh));
        }

        Response.Cookies.Delete(AccessTokenCookie, BuildLegacyCookieOptions());
        Response.Cookies.Delete(RefreshTokenCookie, BuildRefreshCookieOptions());
        Response.Cookies.Delete(RefreshTokenCookie, BuildLegacyCookieOptions());
        return Ok();
    }

    [HttpPost("Rename")]
    public async Task<ActionResult<Response<bool>>> Rename([FromBody] string newName)
    {
        var result = await Mediator.Send(new RenameCharacterCommand(CurrentUserId, newName));
        if (result.Data is null) return BadRequest(result);

        SetAuthCookies(result.Data);
        return Ok(result);
    }

    [HttpPost("createNewTokens")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Response<Tokens>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<Tokens>>> CreateNewTokens()
    {
        if (!HasRefreshCookieCsrfHeader()) return BadRequest();

        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refresh))
            return BadRequest();

        try
        {
            var result = await _refreshTokenRotation.ExecuteAsync(
                refresh,
                () => Mediator.Send(new CreateNewTokensCommand(refresh)));

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

    [HttpGet("getUserInfo")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<UserInfoDto>>> GetCurrentUserInfo() =>
        await Mediator.Send(new GetUserInfoQuery(CurrentUserId));

    private void SetAuthCookies(Tokens t)
    {
        Response.Cookies.Delete(AccessTokenCookie, BuildLegacyCookieOptions());
        Response.Cookies.Delete(RefreshTokenCookie, BuildLegacyCookieOptions());
        Response.Cookies.Append(RefreshTokenCookie, t.RefreshToken, BuildRefreshCookieOptions());
    }

    private CookieOptions BuildRefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookies(),
        IsEssential = true,
        Path = "/api/v1/auth",
        SameSite = UseSecureCookies() ? SameSiteMode.None : SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        MaxAge = TimeSpan.FromDays(365)
        // Domain   = _cfg["HostedDomain"] // ← uncomment if you serve from a sub‑domain
    };

    private CookieOptions BuildLegacyCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookies(),
        IsEssential = true,
        Path = "/",
        SameSite = UseSecureCookies() ? SameSiteMode.None : SameSiteMode.Lax
    };

    private bool UseSecureCookies()
    {
        if (Request.IsHttps) return true;
        if (_env.IsDevelopment() && IsLocalhost(Request.Host.Host)) return false;

        return true;
    }

    private static bool IsLocalhost(string? host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);

    private bool HasRefreshCookieCsrfHeader()
    {
        if (!Request.Headers.TryGetValue(RefreshCookieCsrfHeader, out var values))
        {
            return false;
        }

        return string.Equals(values.ToString(), "1", StringComparison.Ordinal);
    }
}
