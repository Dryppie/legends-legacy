using Application.UseCases.CombatStyles.Models;
using Domain.Models.CombatStyles;

namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleService
{
    Task<CombatStylesOverviewModel> GetOverviewAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CombatStyleOperationResult> ActivateStyleAsync(Guid characterId, string styleId, CancellationToken cancellationToken);
    Task<CombatStyleOperationResult<CombatStyleModel>> SelectFocusAsync(Guid characterId, string styleId, string focusId, CancellationToken cancellationToken);
    Task<CombatStyleOperationResult<CombatStyleModel>> RankUpNodeAsync(Guid characterId, string styleId, string nodeId, CancellationToken cancellationToken);
    Task<CombatStyleOperationResult<CombatStyleModel>> ResetSkillTreeAsync(Guid characterId, string styleId, CancellationToken cancellationToken);
    Task<CombatStyleSnapshot?> GetActiveSnapshotAsync(Guid characterId, CancellationToken cancellationToken);
    Task GrantExperienceAsync(Guid characterId, long amount, string source, CancellationToken cancellationToken);
}
