namespace Application.Interfaces.Services.LL.Quests;

public sealed record QuestCompletionChatMessage(string QuestId, string Title);

public interface IQuestSystemChatPublisher
{
    Task PublishAsync(
        Guid characterId,
        IReadOnlyCollection<QuestCompletionChatMessage> completions,
        CancellationToken cancellationToken);
}
