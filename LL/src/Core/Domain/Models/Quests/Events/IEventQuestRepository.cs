namespace Domain.Models.Quests.Events;

public interface IEventQuestRepository
{
    Task<IReadOnlyList<EventQuestInstance>> GetAllAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EventQuestInstance?> GetAsync(string eventQuestId, Guid characterId, CancellationToken cancellationToken);
    Task<EventQuestContributionStanding> GetContributionStandingAsync(
        string eventQuestId,
        Guid characterId,
        int topCount,
        CancellationToken cancellationToken);
    Task<bool> HasProcessedAsync(string eventQuestId, string objectiveKey, Guid outboxMessageId, CancellationToken cancellationToken);
    void Add(EventQuestInstance instance);
    void AddLedger(EventQuestEventLedger ledger);
    void AddClaim(EventQuestRewardClaim claim);
    void AddMilestoneClaim(EventQuestMilestoneClaim claim);
    Task AddSigilFragmentsAsync(Guid characterId, int amount, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record EventQuestContributionStanding(
    IReadOnlyList<EventQuestContributor> TopContributors,
    int? CharacterRank,
    int ContributorCount,
    long? ContributionToNextRank);

public sealed record EventQuestContributor(
    int Rank,
    Guid CharacterId,
    string CharacterName,
    long Contribution);
