using Domain.Models.CharacterActions.CharacterActionDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CharacterActions;
public class CombatActionDetailsConfiguration : IEntityTypeConfiguration<CombatActionDetails>
{
    public void Configure(EntityTypeBuilder<CombatActionDetails> builder)
    {
        
    }
}
