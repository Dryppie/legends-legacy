using Application.Interfaces.Services.LL;
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

    public async Task<AppUser?> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken)
    {
        if (await _userRepository.FindByEmailAsync(email, cancellationToken) is not null)
            return null;

        var user = AppUser.Register(username, email,
                     _hasher.HashPassword(null!, password));

        var added = await _userRepository.AddAsync(user, cancellationToken);
        if (!added) return null;

        return user;
    }

    public async Task<AppUser?> RegisterGuestAsync(CancellationToken cancellationToken)
    {
        var guest = AppUser.Guest();
        guest.Username = GenerateGuestName();
        await _userRepository.AddAsync(guest, cancellationToken);
        return guest;
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        if (user == null) return null;

        var vr = _hasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (vr != PasswordVerificationResult.Success)
            return null;

        return user;
    }

    public async Task<AppUser?> ConvertGuestToUser(Guid userId, string username, string email, string password, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user == null) return null;
        if (await _userRepository.FindByEmailAsync(email, cancellationToken) != null) return null;


        user.ConvertGuestToAccount(username, email,
                     _hasher.HashPassword(null!, password));
        user.IsGuest = false;

        await _userRepository.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<UserInfo?> GetUserInfo(Guid userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetUserInfo(userId, cancellationToken);
    }

    private string GenerateGuestName()
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
}