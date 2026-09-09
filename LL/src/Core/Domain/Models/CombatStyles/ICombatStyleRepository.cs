namespace Domain.Models.CombatStyles;

public interface ICombatStyleRepository
{
    /// <summary>Changes when the persistence unit of work discards tracked state.</summary>
    long TrackingGeneration => 0;
    Task<IReadOnlyList<CharacterCombatStyle>> GetOwnedAsync(Guid characterId, CancellationToken ct);
    Task<CharacterCombatStyleSelection?> GetSelectionAsync(Guid characterId, CancellationToken ct);
    void Add(CharacterCombatStyle style);
    void Add(CharacterCombatStyleSelection selection);
}
