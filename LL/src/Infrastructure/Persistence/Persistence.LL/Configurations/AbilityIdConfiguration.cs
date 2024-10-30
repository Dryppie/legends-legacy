using Domain.Models.Abilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations;
public class AbilityIdConfiguration : IEntityTypeConfiguration<AbilityId>
{
    public void Configure(EntityTypeBuilder<AbilityId> builder)
    {
        builder.HasKey(ai => new { ai.Id, ai.EntityId});
    }
}