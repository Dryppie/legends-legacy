using Application.Common.Interfaces;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.QueryProfiles;

namespace Persistence.LL.Repositories.Snapshots;

public class CharacterSnapshotRepository : ICharacterSnapshotRepository
{
    private readonly IDbContext _dbContext;

    public CharacterSnapshotRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct = default)
    {
        var character = await _dbContext.Characters
            .AsNoTracking()
            .EntireCharacter()
            .SingleAsync(c => c.Id == characterId, ct);

        // Build "inputs" from persisted state
        var baseAttrs = character.BaseAttributes
            .Select(a => new EntityAttributeSnapshot() { CharacterSnapshotId = a.EntityId, AttributeType = a.AttributeType, Value = a.Value });

        var activeEssenceIds = character.EssenceSlots
            .Where(s => s.SlotState == SlotState.Active && s.EssenceId.HasValue)
            .Select(s => s.EssenceId!.Value)
            .OrderBy(x => x)
            .ToList();

        var equipment = character.EquipmentSlots
                .Where(s => s.EquipmentInstanceId.HasValue && s.EquipmentInstance != null)
                .Select(s => EquipmentSnapshot.From(s.EquipmentSlotType, s.EquipmentInstance!))
                .OrderBy(e => e.Slot)
                .ThenBy(e => e.EquipmentInstanceId)
                .ToList();

        var snapshot = new CharacterSnapshot() {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Name = character.Name,
            Level = character.Level,
            BaseAttributes = [.. baseAttrs],
            ActiveEssenceIds = activeEssenceIds,
            Equipment = equipment
        };

        _dbContext.CharacterSnapshots.Add(snapshot);

        return snapshot;
    }

    public async Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct)
    {
        return await _dbContext.CharacterSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CharacterId == characterId, ct);
    }
}
