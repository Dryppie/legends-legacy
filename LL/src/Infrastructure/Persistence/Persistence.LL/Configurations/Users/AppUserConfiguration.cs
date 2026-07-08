using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Users;
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired();

        builder.Property(e => e.Username)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(e => e.NormalizedUsername)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(320);

        builder.Property(e => e.NormalizedEmail)
            .HasMaxLength(320);

        builder.HasIndex(e => e.NormalizedUsername)
            .IsUnique();

        builder.HasIndex(e => e.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
    }
}
