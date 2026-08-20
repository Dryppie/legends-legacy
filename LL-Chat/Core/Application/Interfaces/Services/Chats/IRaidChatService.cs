namespace Application.Interfaces.Services.Chats;

public interface IRaidChatService
{
    Task<bool> ApplySnapshotAsync(
        Guid raidRunId,
        long revision,
        bool isOpen,
        IReadOnlyCollection<Guid> memberCharacterIds,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);

    Task<bool> CanAccessAsync(
        Guid raidRunId,
        Guid characterId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRecipientsForMemberAsync(
        Guid raidRunId,
        Guid characterId,
        CancellationToken cancellationToken);
}
