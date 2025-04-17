using API.LL.Controllers;
using Application.Common.Responses;
using Application.UseCases.Authorization.Commands.CreateNewTokens;
using Application.UseCases.Authorization.Queries.ValidateToken;
using Application.UseCases.Users.Commands.ConvertGuestToUser;
using Application.UseCases.Users.Commands.GuestLogin;
using Application.UseCases.Users.Commands.Register;
using Application.UseCases.Users.Dtos;
using Application.UseCases.Users.Queries.Login;
using Application.UseCases.Users.Queries.GetUserInfo;
using Common.Authorization.Security;
using Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class AuthController : BaseController
{
    public record UserLoginDto(string Email, string Password);
    public record UserRegisterDto(string Username, string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Response<Tokens>>> Login([FromBody] UserLoginDto input)
    {
        var tokens = await Mediator.Send(new LoginQuery(input.Email, input.Password));
        if (tokens.Data == null) return BadRequest(tokens);

        var cookies = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("AccessToken", tokens.Data.AccessToken),
            new KeyValuePair<string, string>("RefreshToken", tokens.Data.RefreshToken)
        };

        Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("loginAsGuest")]
    public async Task<ActionResult<Tokens>> LoginAsGuest()
    {
        var tokens = await Mediator.Send(new GuestLoginCommand());

        if (tokens.Data == null) return BadRequest(tokens);

        var cookies = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("AccessToken", tokens.Data.AccessToken),
            new KeyValuePair<string, string>("RefreshToken", tokens.Data.RefreshToken)
        };

        Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

        return Ok(tokens);
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
    public async Task<ActionResult<Response<Unit>>> Register([FromBody] UserRegisterDto input)
    {
        var response = await Mediator.Send(new RegisterCommand(input.Username, input.Email, input.Password));

        return Ok(response);
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
    public async Task<ActionResult<Response<Tokens>>> CreateNewTokens()
    {
        var refreshToken = HttpContext.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest();

        try
        {
            var tokens = await Mediator.Send(new CreateNewTokensCommand(refreshToken));
            if (tokens.Data == null) return BadRequest(tokens);

            var cookies = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("AccessToken", tokens.Data.AccessToken),
                new KeyValuePair<string, string>("RefreshToken", tokens.Data.RefreshToken)
            };

            Response.Cookies.Append(cookies.ToArray(), GetCookieOptions());

            return Ok(tokens);
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

    [HttpGet("getUserInfo")]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUserInfo()
    {
        return await Mediator.Send(new GetUserInfoQuery(CurrentUserId));
    }

    private CookieOptions GetCookieOptions() => new()
    {
        Secure = true,
        HttpOnly = true,
        Path = "/",
        //Domain = _configuration["HostedDomain"], // the top level domain 
        SameSite = IsLocal() ? SameSiteMode.None : SameSiteMode.Strict,
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        MaxAge = TimeSpan.FromDays(365),
    };
}