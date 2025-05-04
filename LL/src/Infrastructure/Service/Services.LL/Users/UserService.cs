using Application.Interfaces.Services.LL;
using Common.Authorization.Security;
using Common.Exceptions;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace Services.LL.Users;
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<AppUser> _hasher;

    public UserService(IUserRepository userRepository, IPasswordHasher<AppUser> hasher)
    {
        _userRepository = userRepository;
        _hasher = hasher;
    }

    public async Task<AppUser> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken)
    {
        if (await _userRepository.FindByEmailAsync(email, cancellationToken) is not null)
            throw new InvalidOperationException("E‑mail already used");

        var user = AppUser.Register(username, email,
                     _hasher.HashPassword(null!, password));

        await _userRepository.AddAsync(user, cancellationToken);
        return user;
    }

    public async Task<AppUser> RegisterGuestAsync(CancellationToken cancellationToken)
    {
        var guest = AppUser.Guest();
        guest.Username = GenerateGuestName();
        await _userRepository.AddAsync(guest, cancellationToken);
        return guest;
    }

    public async Task<AppUser> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken)
                   ?? throw new InvalidOperationException("Invalid login");

        var vr = _hasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (vr != PasswordVerificationResult.Success)
            throw new InvalidOperationException("Invalid login");

        return user;
    }

    //// inheritdoc />
    //public async Task<AuthInfo> Login(string email, string password)
    //{
    //    // Throw NotFound exception if no user is found by the specified email
    //    var user = await _userManager.FindByEmailAsync(email) ?? throw new NotFoundException();

    //    // Return the found user if the given password is valid for said user, else throw NotFound exception
    //    if (await _userManager.CheckPasswordAsync(user, password))
    //    {
    //        return new AuthInfo
    //        {
    //            IsValid = true,
    //            Id = user.Id,
    //            Name = user.UserName!,
    //            IsPlayer = true,
    //        };
    //    }

    //    throw new NotFoundException();
    //}

    //public async Task<AuthInfo> Register(string username, string email, string password)
    //{
    //    if (_userRepository.DoesUsernameExist(username)) throw new Exception("Username has already been used.");
    //    if (_userRepository.DoesEmailExist(email)) throw new Exception("Email has already been used.");

    //    var user = new AppUser { UserName = username, Email = email };

    //    var result = await _userManager.CreateAsync(user, password);

    //    return new AuthInfo
    //    {
    //        IsValid = true,
    //        Id = user.Id,
    //        Name = user.UserName,
    //    };
    //}

    public string GenerateGuestName()
    {
        var prefixes = new[]
            {
                "Silent", "Swift", "Mighty", "Lucky", "Clever", "Brave", "Gentle", "Fierce", "Bold", "Wild",
                "Calm", "Stormy", "Vivid", "Bright", "Dark", "Noble", "Proud", "Shy", "Quick", "Sly",
                "Lone", "Nimble", "Radiant", "Wise", "Cheerful", "Eager", "Mystic", "Fearless", "Daring", "Joyful",
                "Steady", "Thunder", "Majestic", "Silent", "Sparkling", "Serene", "Stout", "Loyal", "Iron", "Fiery"
            };

        var animals = new[]
        {
                "Fox", "Bear", "Tiger", "Hawk", "Lion", "Wolf", "Otter", "Eagle", "Panther", "Falcon",
                "Raven", "Shark", "Puma", "Cobra", "Jaguar", "Leopard", "Bison", "Lynx", "Cougar", "Phoenix",
                "Owl", "Dragon", "Unicorn", "Griffin", "Raccoon", "Badger", "Cheetah", "Stag", "Rhino", "Lizard",
                "Antelope", "Gazelle", "Ram", "Horse", "Buffalo", "Beetle", "Whale", "Spider", "Wolverine", "Elephant"
            };

        var suffixes = new[]
        {
                "Walker", "Seeker", "Rider", "Hunter", "Keeper", "Wanderer", "Dreamer", "Protector", "Guardian", "Voyager",
                "Strider", "Glider", "Howler", "Whisperer", "Tracker", "Scout", "Mage", "Sentinel", "Scribe", "Knight",
                "Mystic", "Sailor", "Pathfinder", "Warrior", "Champion", "Explorer", "Savior", "Adventurer", "Defender", "Scout",
                "Challenger", "Nomad", "Beholder", "Sorcerer", "Alchemist", "Master", "Scribe", "Scholar", "Pilgrim", "Ranger"
            };

        var random = new Random();
        var prefix = prefixes[random.Next(prefixes.Length)];
        var animal = animals[random.Next(animals.Length)];
        var suffix = suffixes[random.Next(suffixes.Length)];

        return $"{prefix}{animal}{suffix}_{random.Next(1000, 9999)}";
    }

    //public async Task<AuthInfo> ConvertGuestToUser(string userId, string username, string email, string password)
    //{
    //    if (!_userRepository.DoesGuestExist(userId)) throw new Exception("User does not exist");
    //    if (_userRepository.DoesEmailExist(email)) throw new Exception("Email is already in use");

    //    var user = await _userManager.FindByIdAsync(userId);
    //    if (user == null)
    //    {
    //        return new AuthInfo { IsValid = false };
    //    }

    //    await _userManager.AddPasswordAsync(user, password);
    //    await _userManager.SetEmailAsync(user, email);
    //    await _userManager.SetUserNameAsync(user, username);
    //    await _userManager.UpdateNormalizedUserNameAsync(user);

    //    user.IsGuest = false;

    //    await _userManager.UpdateAsync(user);

    //    return new AuthInfo
    //    {
    //        IsValid = true,
    //        Id = userId,
    //        Name = username,
    //        IsPlayer = true
    //    };
    //}

    //public async Task<UserInfo> GetUserInfo(Guid userId)
    //{
    //    return await _userRepository.GetUserInfo(userId);
    //}
}