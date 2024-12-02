using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations;
public class EssenceConfiguration : IEntityTypeConfiguration<Essence>
{
    public void Configure(EntityTypeBuilder<Essence> builder)
    {
        builder.HasKey(e => e.Id);
    }
}