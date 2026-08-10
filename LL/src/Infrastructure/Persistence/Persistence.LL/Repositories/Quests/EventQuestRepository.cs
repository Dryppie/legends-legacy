using Application.Common.Interfaces;
using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Quests;

public sealed class EventQuestRepository(IDbContext context) : IEventQuestRepository
{
    public async Task<IReadOnlyList<EventQuestInstance>> GetAllAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.EventQuestInstances
            .Include(x => x.Objectives)
            .Include(x => x.Contributions.Where(contribution => contribution.CharacterId == characterId))
            .Include(x => x.RewardClaims.Where(claim => claim.CharacterId == characterId))
            .Include(x => x.MilestoneClaims.Where(claim => claim.CharacterId == characterId))
            .ToListAsync(cancellationToken);

    public Task<EventQuestInstance?> GetAsync(
        string eventQuestId,
        Guid characterId,
        CancellationToken cancellationToken) =>
        context.EventQuestInstances
            .Include(x => x.Objectives)
            .Include(x => x.Contributions.Where(contribution => contribution.CharacterId == characterId))
            .Include(x => x.RewardClaims.Where(claim => claim.CharacterId == characterId))
            .Include(x => x.MilestoneClaims.Where(claim => claim.CharacterId == characterId))
            .SingleOrDefaultAsync(x => x.EventQuestId == eventQuestId, cancellationToken);

    public async Task<EventQuestContributionStanding> GetContributionStandingAsync(
        string eventQuestId,
        Guid characterId,
        int topCount,
        CancellationToken cancellationToken)
    {
        var contributions = context.EventQuestCharacterContributions
            .AsNoTracking()
            .Where(x => x.EventQuestId == eventQuestId);
        var contributorCount = await contributions.CountAsync(cancellationToken);
        var characterContribution = await contributions
            .Where(x => x.CharacterId == characterId)
            .Select(x => (long?)x.TotalAmount)
            .SingleOrDefaultAsync(cancellationToken);

        int? characterRank = null;
        long? contributionToNextRank = null;
        if (characterContribution.HasValue)
        {
            characterRank = await contributions.CountAsync(
                x => x.TotalAmount > characterContribution.Value,
                cancellationToken) + 1;
            var nextContribution = await contributions
                .Where(x => x.TotalAmount > characterContribution.Value)
                .OrderBy(x => x.TotalAmount)
                .Select(x => (long?)x.TotalAmount)
                .FirstOrDefaultAsync(cancellationToken);
            contributionToNextRank = nextContribution.HasValue
                ? nextContribution.Value - characterContribution.Value
                : null;
        }

        var topRows = await (
                from contribution in contributions
                join character in context.Characters.AsNoTracking()
                    on contribution.CharacterId equals character.Id
                orderby contribution.TotalAmount descending,
                    contribution.LastContributedAt,
                    contribution.CharacterId
                select new
                {
                    contribution.CharacterId,
                    CharacterName = character.Name,
                    Contribution = contribution.TotalAmount
                })
            .Take(Math.Max(0, topCount))
            .ToListAsync(cancellationToken);
        var topContributors = topRows
            .Select((row, index) => new EventQuestContributor(
                index + 1,
                row.CharacterId,
                row.CharacterName,
                row.Contribution))
            .ToList();

        return new EventQuestContributionStanding(
            topContributors,
            characterRank,
            contributorCount,
            contributionToNextRank);
    }

    public Task<bool> HasProcessedAsync(
        string eventQuestId,
        string objectiveKey,
        Guid outboxMessageId,
        CancellationToken cancellationToken) =>
        context.EventQuestEventLedgers.AnyAsync(
            x => x.EventQuestId == eventQuestId &&
                 x.ObjectiveKey == objectiveKey &&
                 x.OutboxMessageId == outboxMessageId,
            cancellationToken);

    public void Add(EventQuestInstance instance) => context.EventQuestInstances.Add(instance);
    public void AddLedger(EventQuestEventLedger ledger) => context.EventQuestEventLedgers.Add(ledger);
    public void AddClaim(EventQuestRewardClaim claim) => context.EventQuestRewardClaims.Add(claim);
    public void AddMilestoneClaim(EventQuestMilestoneClaim claim) => context.EventQuestMilestoneClaims.Add(claim);

    public async Task AddSigilFragmentsAsync(
        Guid characterId,
        int amount,
        CancellationToken cancellationToken)
    {
        var character = await context.Characters.SingleAsync(x => x.Id == characterId, cancellationToken);
        character.SigilFragments += amount;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
