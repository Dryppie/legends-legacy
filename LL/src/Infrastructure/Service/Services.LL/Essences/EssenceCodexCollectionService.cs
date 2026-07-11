using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Essences;

public sealed class EssenceCodexCollectionService : IEssenceCodexCollectionService
{
    private readonly IDbContext _dbContext;
    private readonly IEssenceCodexCollectionDefinitionProvider _collectionDefinitions;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;

    public EssenceCodexCollectionService(
        IDbContext dbContext,
        IEssenceCodexCollectionDefinitionProvider collectionDefinitions,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _dbContext = dbContext;
        _collectionDefinitions = collectionDefinitions;
        _essenceDefinitions = essenceDefinitions;
    }

    public async Task<IReadOnlyList<EssenceCodexEntry>> GetVisibleEntriesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var absorbedEssences = await GetAbsorbedEssencesAsync(characterId, cancellationToken);

        return _collectionDefinitions.GetAll()
            .Where(collection => collection.EssenceDefinitionIds.Any(absorbedEssences.ContainsKey))
            .Select(collection => CreateEntry(collection, absorbedEssences))
            .OrderBy(entry => entry.IsUnlocked ? 0 : 1)
            .ThenBy(entry => entry.Category)
            .ThenBy(entry => entry.Title)
            .ToList();
    }

    private async Task<IReadOnlyDictionary<string, int>> GetAbsorbedEssencesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var essences = await _dbContext.PlayerEssences
            .Where(x => x.CharacterId == characterId)
            .Select(x => new
            {
                x.EssenceDefinitionId,
                x.AscensionTier
            })
            .ToListAsync(cancellationToken);

        return essences
            .GroupBy(x => x.EssenceDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Max(x => x.AscensionTier),
                StringComparer.OrdinalIgnoreCase);
    }

    private EssenceCodexEntry CreateEntry(
        EssenceCodexCollectionDefinition collection,
        IReadOnlyDictionary<string, int> absorbedEssences)
    {
        var members = collection.EssenceDefinitionIds
            .Select(id =>
            {
                var definition = _essenceDefinitions.GetById(id);
                var isAbsorbed = absorbedEssences.TryGetValue(id, out var ascensionTier);
                return new EssenceCodexMember(
                    id,
                    definition?.Name ?? FormatEssenceName(id),
                    isAbsorbed,
                    isAbsorbed ? ascensionTier : 0);
            })
            .ToList();

        var current = members.Count(member => member.IsAbsorbed);
        var required = members.Count;
        var isUnlocked = current >= required;
        var collectionAscensionTier = isUnlocked
            ? members.Min(member => member.AscensionTier)
            : 0;
        var bonusValue = collection.Bonus.Value +
            collectionAscensionTier * collection.Bonus.ValuePerCollectionAscensionTier;

        return new EssenceCodexEntry(
            collection.Id,
            collection.Title,
            collection.Description,
            collection.Bonus.Description,
            collection.Bonus.Kind,
            collection.Bonus.Value,
            bonusValue,
            collection.Bonus.ValuePerCollectionAscensionTier,
            collectionAscensionTier,
            EssenceProgressionConstants.MaxAscensionTier,
            current,
            required,
            isUnlocked,
            collection.Category,
            members);
    }

    private static string FormatEssenceName(string essenceDefinitionId)
    {
        const string essencePrefix = "essence.";
        var raw = essenceDefinitionId.StartsWith(essencePrefix, StringComparison.OrdinalIgnoreCase)
            ? essenceDefinitionId[essencePrefix.Length..]
            : essenceDefinitionId;

        return string.Join(' ', raw
            .Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
