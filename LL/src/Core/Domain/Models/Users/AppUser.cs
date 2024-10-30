using Domain.Models.Users.IPAddresses;
using Domain.Models.Users.Transactions;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models.Users;
public class AppUser : IdentityUser
{
    public DateTimeOffset? BannedUntil { get; set; }
    public ICollection<IPAddress> IPAddresses { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
}