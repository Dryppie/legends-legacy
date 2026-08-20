using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Chat.Configurations;

public sealed class RaidChatChannelConfiguration : IEntityTypeConfiguration<RaidChatChannel>
{
    public void Configure(EntityTypeBuilder<RaidChatChannel> builder)
    {
        builder.ToTable("RaidChatChannels");
        builder.HasKey(x => x.RaidRunId);
        builder.HasMany(x => x.Memberships)
            .WithOne(x => x.Channel)
            .HasForeignKey(x => x.RaidRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.IsOpen, x.UpdatedAt });
    }
}

public sealed class RaidChatMembershipConfiguration : IEntityTypeConfiguration<RaidChatMembership>
{
    public void Configure(EntityTypeBuilder<RaidChatMembership> builder)
    {
        builder.ToTable("RaidChatMemberships");
        builder.HasKey(x => new { x.RaidRunId, x.CharacterId });
        builder.HasIndex(x => x.CharacterId);
    }
}
