using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Authorization.Interfaces;
using Application.Interfaces.Services.LL.Entities;
using Common.Authorization.Security;
using Common.Options;
using Domain.Models.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Services.LL.Authorization;
public class JwtGenerator : IJwtGenerator
{
    private readonly IRefreshTokenRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly ITokenHasher _hasher;
    private readonly ICharacterService _characterService;

    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TokenValidationParameters _parameters;

    private readonly TimeSpan _accessLifespan;
    private readonly TimeSpan _refreshLifespan;
    private readonly string _validIssuer;
    public readonly string _validAudience;
    public JwtGenerator(IRefreshTokenRepository repo, IUserRepository userRepo, ITokenHasher hasher, ICharacterService characterService, IOptions<JwtOptions> jwtOpt)
    {
        _repo = repo;
        _userRepo = userRepo;
        _hasher = hasher;
        _characterService = characterService;

        var opt = jwtOpt.Value;

        _accessLifespan = TimeSpan.FromMinutes(opt.AccessMinutes);
        _refreshLifespan = TimeSpan.FromDays(opt.RefreshDays);
        _validIssuer = opt.Issuer;
        _validAudience = opt.Audience;
        _signingKey = GetSymmetricSecurityKey(opt.SigningKey);

        _parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = _validIssuer,
            ValidAudience = _validAudience,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromSeconds(10)
        };
    }

    public Tokens IssueTokens(AppUser user)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("guest",                       user.IsGuest.ToString()),
            new(ClaimTypes.UserData,           user.Id.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(user.CharacterId.ToString()))
            claims.Add(new Claim("CharacterId", user.CharacterId.ToString()!));

        var jwt = new JwtSecurityToken(
            issuer: _validIssuer,
            audience: _validAudience,
            claims: claims,
            notBefore: now,
            expires: now.Add(_accessLifespan),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        );

        var access = _handler.WriteToken(jwt);
        var refresh = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _hasher.Hash(refresh),
            ExpiresUtc = now.Add(_refreshLifespan)
        };

        _repo.AddAsync(refreshEntity, CancellationToken.None).GetAwaiter().GetResult();

        return new Tokens(access, refresh);
    }

    public async Task<Tokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var record = await _repo.FindAsync(refreshToken, cancellationToken);

        if (record == null) return null;
        if (!record.IsActive) return null;

        // revoke current token, issue new pair
        record.RevokedUtc = DateTime.UtcNow;

        var userId = record.UserId; // needed for new token

        var user = await _userRepo.FindByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        var character = await _characterService.GetMyCharacterAsync(user.Id, cancellationToken);
        if (character == null) return null;

        user.CharacterId = character.Id;

        var newTokens = IssueTokens(user);
        // store the hash of the *new* refresh token inside the old one (`ReplacedBy`) – optional
        record.ReplacedBy = _hasher.Hash(newTokens.RefreshToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return newTokens;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAccessToken(string token)
    {
        try
        {
            await _handler.ValidateTokenAsync(token, _parameters);
            return true;
        }
        catch
        {
            return false; // expired, malformed, wrong key, etc.
        }
    }

    private static SymmetricSecurityKey GetSymmetricSecurityKey(string key)
    {
        return new(Encoding.UTF8.GetBytes(key));
    }
}