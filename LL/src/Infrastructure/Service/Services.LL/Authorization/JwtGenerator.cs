using Application.Authorization.Interfaces;
using Application.Common.Interfaces;
using Common.Authorization.Security;
using Common.DateTimeProvider;
using Common.Exceptions;
using Domain.Models.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.LL.Authorization;
public class JwtGenerator : IJwtGenerator
{
    private readonly IDateTimeProviderService _dateTimeProvider;
    private readonly IDbContext _context;
    private readonly string _topSecretAccessKey;
    private readonly string _topSecretRefreshKey;
    public JwtGenerator(IDateTimeProviderService dateTimeProvider, IDbContext context, IConfiguration config)
    {
        _dateTimeProvider = dateTimeProvider;
        _context = context;
        _topSecretAccessKey = config.GetSection("Jwt").GetValue<string>("AccessTokenSecretKey")!;
        _topSecretRefreshKey = config.GetSection("Jwt").GetValue<string>("RefreshTokenSecretKeyV2")!;
    }

    public string CreateAccessToken(AppUser appUser)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.UserData, appUser.Id),
            new Claim(ClaimTypes.Name, appUser.UserName!),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_topSecretAccessKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string CreateRefreshToken(AppUser appUser)
    {
        throw new NotImplementedException();
    }

    public Tokens GenerateTokens(AuthInfo authInfo)
    {
        var accessTokenClaims = GenerateAccessTokenClaims(authInfo);
        var accessToken = GenerateToken(accessTokenClaims, _topSecretAccessKey, "", "", 60 * 12);

        var refreshTokenClaims = GenerateRefreshTokenClaims(authInfo);
        var refreshToken = GenerateToken(refreshTokenClaims, _topSecretRefreshKey, "", "", 60 * 12 * 30);
        return new Tokens(accessToken, refreshToken);
    }

    private string GenerateToken(List<Claim> claims, string key, string issuer, string audience, long expires)
    {
        var expire = _dateTimeProvider
            .Now()
            .AddMinutes(expires)
            .UtcDateTime;

        var credentials = new SigningCredentials(GetSymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new JwtSecurityToken(issuer, audience, claims,
            expires: expire, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private static List<Claim> GenerateAccessTokenClaims(AuthInfo info)
    {
        return new()
        {
            new(ClaimTypes.UserData, info.Id),
            new(ClaimTypes.Name, info.Name),
            new("CharacterId", info.CharacterId)
        };
    }

    private List<Claim> GenerateRefreshTokenClaims(AuthInfo info)
    {
        return new()
        {
            new(ClaimTypes.UserData, info.Id),
            new Claim("iat", _dateTimeProvider.Now().ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };
    }


    /// <inheritdoc />
    public Tokens RefreshTokens(string refreshToken, bool isRefreshFromWeb = false)
    {
        var isNewRefreshTokenValid = ValidateToken(_topSecretRefreshKey, "", "", refreshToken);

        if (!isNewRefreshTokenValid)
        {
            //_logger.LogWarning("Attempt to refresh invalid or expired refresh token {RefreshToken}", refreshToken);
            //throw new InvalidRefreshTokenException("Invalid refresh token");
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(refreshToken);

        var claims = jwtSecurityToken.Claims.ToList();
        var id = claims.SingleOrDefault(x => x.Type == ClaimTypes.UserData)!.Value;
        var user = _context.Users
            .FirstOrDefault(x => x.Id == id && !x.BannedUntil.HasValue);
        NotFoundException.ThrowIfNull(user, nameof(AppUser), id);



        var userIsCurrentlyBanned = user is null;
        if (userIsCurrentlyBanned)
        {
            //_logger.LogWarning("Attempt to refresh tokens for deleted or soft-deleted user {Id}", id);
            //throw new InvalidRefreshTokenException("Cannot issue new tokens for deleted or softdeleted users");
        }

        //if (member!.RefreshTokensRevokedBefore.HasValue && jwtSecurityToken.IssuedAt <= member!.RefreshTokensRevokedBefore.Value)
        //{
        //    _logger.LogWarning("Attempt to refresh tokens for user with {RefreshTokensRevokedBefore} for {Id}", member.RefreshTokensRevokedBefore, id);
        //    throw new InvalidRefreshTokenException("Cannot issue new tokens with refresh token that has been revoked");
        //}

        var authInfo = new AuthInfo()
        {
            Id = id,
            Name = user!.UserName!,
        };

        return GenerateTokens(authInfo);
    }

    /// <inheritdoc />
    public bool ValidateAccessToken(string token)
    {
        return ValidateToken(_topSecretAccessKey, "", "", token);
    }

    internal static bool ValidateToken(string key, string issuer, string audience, string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            tokenHandler.ValidateToken(token,
                new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetSymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    //ValidIssuer = issuer,
                    //ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero,
                }, out var validatedToken);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static SymmetricSecurityKey GetSymmetricSecurityKey(string key)
    {
        return new(Encoding.UTF8.GetBytes(key));
    }
}