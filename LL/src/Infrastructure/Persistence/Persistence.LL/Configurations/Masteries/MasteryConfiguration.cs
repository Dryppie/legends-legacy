using Domain.Models.Masteries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Masteries;
public class MasteryConfiguration : IEntityTypeConfiguration<Mastery>
{
    public void Configure(EntityTypeBuilder<Mastery> builder)
    {
        builder.HasKey(m => new { m.EntityId, m.MasteryType });
    }
}