using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Chat.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasIndex(message => new
        {
            message.SenderId,
            message.IsSystemGenerated,
            message.SentAt,
            message.Id
        });

        builder.HasIndex(message => new
            {
                message.ChannelType,
                message.SenderId,
                message.TargetCharacterId,
                message.IsSystemGenerated,
                message.SentAt,
                message.Id
            })
            .HasDatabaseName("IX_ChatMessages_DirectConversationForward");

        builder.HasIndex(message => new
            {
                message.ChannelType,
                message.TargetCharacterId,
                message.SenderId,
                message.IsSystemGenerated,
                message.SentAt,
                message.Id
            })
            .HasDatabaseName("IX_ChatMessages_DirectConversationReverse");

        builder.HasIndex(message => new
            {
                message.ChannelType,
                message.SenderId,
                message.IsSystemGenerated,
                message.SentAt,
                message.ContextKey
            })
            .HasDatabaseName("IX_ChatMessages_SharedChannelEvidence");
    }
}
