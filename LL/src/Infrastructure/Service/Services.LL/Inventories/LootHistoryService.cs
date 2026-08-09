using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Inventories;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.LootHistory.Dtos;
using Domain.Models.LootHistory;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Services.LL.Inventories;

public sealed class LootHistoryService : ILootHistoryService
{
    public const int MaximumEntriesReturned = 50;

    private readonly IDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeProvider _timeProvider;

    public LootHistoryService(
        IDbContext context,
        JsonSerializerOptions jsonOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions(jsonOptions)
        {
            // Derived equipment and essence DTOs can emit their discriminator after
            // ordinary properties. Existing history snapshots must remain readable.
            AllowOutOfOrderMetadataProperties = true
        };
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<LootHistoryEntryDto>> GetRecentAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var entries = await _context.LootHistoryEntries
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.ReceivedAt)
            .ThenByDescending(x => x.Id)
            .Take(MaximumEntriesReturned)
            .ToListAsync(cancellationToken);

        return entries.Select(ToDto).ToList();
    }

    public async Task RecordAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItemDto> items,
        string source,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var receivedAt = _timeProvider.GetUtcNow();
        var normalizedSource = string.IsNullOrWhiteSpace(source)
            ? "unknown"
            : source.Trim();

        var entries = items.Select(item => new LootHistoryEntry
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            ItemSnapshotJson = JsonSerializer.Serialize(item, _jsonOptions),
            Source = normalizedSource,
            ReceivedAt = receivedAt
        });

        await _context.LootHistoryEntries.AddRangeAsync(entries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ClearAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entries = await _context.LootHistoryEntries
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return 0;
        }

        _context.LootHistoryEntries.RemoveRange(entries);
        await _context.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }

    private LootHistoryEntryDto ToDto(LootHistoryEntry entry)
    {
        var item = JsonSerializer.Deserialize<InventoryItemDto>(
            entry.ItemSnapshotJson,
            _jsonOptions) ?? throw new JsonException(
            $"Loot history entry '{entry.Id}' has an invalid item snapshot.");

        return new LootHistoryEntryDto(entry.Id, item, entry.Source, entry.ReceivedAt);
    }
}
