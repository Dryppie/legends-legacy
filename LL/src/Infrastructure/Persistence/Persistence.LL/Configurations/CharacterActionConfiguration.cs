using Domain.Models.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Models.CharacterActions;

namespace Persistence.LL.Configurations;
public class CharacterActionConfiguration : IEntityTypeConfiguration<CharacterAction>
{
    public void Configure(EntityTypeBuilder<CharacterAction> builder)
    {
        builder.HasKey(e => e.CharacterId);
        builder.HasOne(ca => ca.ActionDetails)
               .WithOne();

        builder.Property(e => e.CharacterId).IsRequired();

    }
}