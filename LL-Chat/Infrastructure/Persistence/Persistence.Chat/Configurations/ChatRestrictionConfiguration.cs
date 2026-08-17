using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Chat.Configurations;

public sealed class ChatRestrictionConfiguration : IEntityTypeConfiguration<ChatRestriction>
{
    public void Configure(EntityTypeBuilder<ChatRestriction> builder)
    {
        builder.ToTable("ChatRestrictions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.CreatedBySubject).HasMaxLength(320).IsRequired();
        builder.Property(x => x.RevokedBySubject).HasMaxLength(320);
        builder.Property(x => x.RevocationReason).HasMaxLength(1_000);
        builder.HasIndex(x => new { x.TargetCharacterId, x.RevokedAt, x.ExpiresAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
