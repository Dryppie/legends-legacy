using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CombatStyles;
using Microsoft.Extensions.DependencyInjection;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleMutationBoundary(
    ICombatStyleActivityRepository activities,
    IServiceProvider scopedServices,
    ICombatStyleRepository? styles = null) : ICombatStyleMutationBoundary
{
    private readonly HashSet<Guid> _settled = [];
    private long _trackingGeneration = styles?.TrackingGeneration ?? 0;

    public async Task<string?> PrepareMutationAsync(Guid characterId, CancellationToken ct)
    {
        if (styles is not null && _trackingGeneration != styles.TrackingGeneration)
        {
            _settled.Clear();
            _trackingGeneration = styles.TrackingGeneration;
        }
        if (await activities.GetCommittedActivityAsync(characterId, ct) is { } activity)
            return $"Finish your {activity} before changing your combat build.";

        // Resolve lazily: idle combat itself resolves styles. Both use the same command scope,
        // repositories and transaction, without a constructor cycle or a second settlement.
        if (!_settled.Contains(characterId))
        {
            var action = await scopedServices.GetRequiredService<ICharacterActionService>()
                .GetCharacterActionAsync(characterId, ct);
            if (action?.HasMoreDueWork == true)
                return "Your pending combat is still resolving. Try changing the build after it finishes.";
            _settled.Add(characterId);
        }
        return null;
    }
}
