namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleActivityRepository
{
    Task<string?> GetCommittedActivityAsync(Guid characterId, CancellationToken ct);
}
