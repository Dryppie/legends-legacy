using Domain.Models.Colosseum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum;

public sealed class ColosseumMatchResultConfiguration : IEntityTypeConfiguration<ColosseumMatchResult>
{
    public void Configure(EntityTypeBuilder<ColosseumMatchResult> builder) =>
        builder.HasIndex(x => x.PlayedAt).IsCreatedConcurrently();
}
