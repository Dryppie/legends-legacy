using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class AdminActionPreviewConfiguration
    : IEntityTypeConfiguration<AdminActionPreview>
{
    public void Configure(EntityTypeBuilder<AdminActionPreview> builder)
    {
        builder.ToTable("AdminActionPreviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionKind).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorSubject).HasMaxLength(320).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.StateHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContextJson).HasMaxLength(2_000).IsRequired();
        builder.HasIndex(x => new { x.OperationId, x.ActionKind });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
