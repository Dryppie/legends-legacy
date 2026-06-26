using Application.Common.Interfaces;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;

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
        var snapshotId = Guid.NewGuid();
        var character = await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.BaseAttributes)
            .Include(c => c.EquipmentSlots)
                .ThenInclude(es => es.EquipmentInstance)
                    .ThenInclude(ei => ei.InstanceModifiers)
            .Include(c => c.EssenceLoadouts.Where(x => x.IsActive))
                .ThenInclude(x => x.Slots)
                    .ThenInclude(x => x.PlayerEssence)
            .SingleAsync(c => c.Id == characterId, ct);

        var baseAttrs = character.BaseAttributes
            .Select(a => new EntityAttributeSnapshot { CharacterSnapshotId = snapshotId, AttributeType = a.AttributeType, Value = a.Value });

        var equipment = character.EquipmentSlots
            .Where(s => s.EquipmentInstanceId.HasValue && s.EquipmentInstance != null)
            .Select(s => EquipmentSnapshot.From(s.EquipmentSlotType, s.EquipmentInstance!))
            .OrderBy(e => e.Slot)
            .ThenBy(e => e.EquipmentInstanceId)
            .ToList();

        var equippedEssences = character.EssenceLoadouts
            .Where(x => x.IsActive)
            .SelectMany(x => x.Slots)
            .Where(x => x.PlayerEssenceId.HasValue && x.PlayerEssence is not null)
            .Select(x => EquippedEssenceSnapshot.From(snapshotId, x.SlotIndex, x.PlayerEssence!))
            .OrderBy(x => x.SlotIndex)
            .ToList();

        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = character.Id,
            Name = character.Name,
            Level = character.Level,
            BaseAttributes = [.. baseAttrs],
            Equipment = equipment,
            EquippedEssences = equippedEssences
        };

        _dbContext.CharacterSnapshots.Add(snapshot);

        return snapshot;
    }

    public async Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct)
    {
        return await _dbContext.CharacterSnapshots
            .AsNoTracking()
            .Include(x => x.BaseAttributes)
            .Include(x => x.Equipment)
                .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.EquippedEssences)
            .FirstOrDefaultAsync(s => s.CharacterId == characterId, ct);
    }

    public async Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        return await _dbContext.CharacterSnapshots
            .AsNoTracking()
            .Include(x => x.BaseAttributes)
            .Include(x => x.Equipment)
                .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.EquippedEssences)
            .FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
    }
}
