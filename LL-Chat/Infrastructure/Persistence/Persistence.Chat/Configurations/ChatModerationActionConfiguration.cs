using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Chat.Configurations;

public sealed class ChatModerationActionConfiguration : IEntityTypeConfiguration<ChatModerationAction>
{
    public void Configure(EntityTypeBuilder<ChatModerationAction> builder)
    {
        builder.ToTable("ChatModerationActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.ActorSubject).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1_000).IsRequired();
        builder.HasIndex(x => new { x.TargetCharacterId, x.OccurredAt });
        builder.HasIndex(x => x.RestrictionId);
        builder.HasIndex(x => x.OccurredAt);
    }
}
