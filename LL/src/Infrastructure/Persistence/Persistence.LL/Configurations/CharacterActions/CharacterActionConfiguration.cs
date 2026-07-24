using Domain.Models.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Persistence.LL.Configurations.CharacterActions;
public class CharacterActionConfiguration : IEntityTypeConfiguration<CharacterAction>
{
    public void Configure(EntityTypeBuilder<CharacterAction> builder)
    {
        builder.HasKey(e => e.CharacterId);

        builder.HasOne(ca => ca.ActionDetails)
            .WithOne()
            .HasForeignKey<ActionDetails>(ad => ad.CharacterActionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.Character)
            .WithOne(c => c.CharacterAction)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CharacterId).IsRequired();
        builder.Property(e => e.RowVersion).IsConcurrencyToken();
    }
}
