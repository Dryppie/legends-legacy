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
        var absorbedIds = await GetAbsorbedEssenceDefinitionIdsAsync(characterId, cancellationToken);

        return _collectionDefinitions.GetAll()
            .Where(collection => collection.EssenceDefinitionIds.Any(absorbedIds.Contains))
            .Select(collection => CreateEntry(collection, absorbedIds))
            .OrderBy(entry => entry.IsUnlocked ? 0 : 1)
            .ThenBy(entry => entry.Category)
            .ThenBy(entry => entry.Title)
            .ToList();
    }

    private async Task<HashSet<string>> GetAbsorbedEssenceDefinitionIdsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var ids = await _dbContext.PlayerEssences
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.EssenceDefinitionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private EssenceCodexEntry CreateEntry(
        EssenceCodexCollectionDefinition collection,
        HashSet<string> absorbedIds)
    {
        var members = collection.EssenceDefinitionIds
            .Select(id =>
            {
                var definition = _essenceDefinitions.GetById(id);
                return new EssenceCodexMember(
                    id,
                    definition?.Name ?? FormatEssenceName(id),
                    absorbedIds.Contains(id));
            })
            .ToList();

        var current = members.Count(member => member.IsAbsorbed);
        var required = members.Count;

        return new EssenceCodexEntry(
            collection.Id,
            collection.Title,
            collection.Description,
            collection.Bonus.Description,
            collection.Bonus.Kind,
            collection.Bonus.Value,
            current,
            required,
            current >= required,
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
