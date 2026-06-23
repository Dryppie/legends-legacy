namespace Domain.Models.Dungeons.Mastery;

public interface ICharacterDungeonMasteryRepository
{
    Task AddAsync(CharacterDungeonMastery mastery, CancellationToken cancellationToken);
    Task<CharacterDungeonMastery?> GetAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterDungeonMastery>> GetForCharacterAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken);
}
