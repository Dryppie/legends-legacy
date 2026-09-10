using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Essences;
using Services.LL.Combat.Engine;

namespace Services.LL.CombatStyles;

public sealed class ChanneledEssenceResolver(
    IEssenceDefinitionRepository essenceDefinitions,
    IAbilityCatalogProvider abilities) : IChanneledEssenceResolver
{
    public ChanneledEssenceOption Resolve(PlayerEssence essence)
    {
        var definition = essenceDefinitions.GetById(essence.EssenceDefinitionId);
        if (definition is null)
            return new(essence.Id, essence.EssenceDefinitionId, essence.EssenceDefinitionId, "", 0, false, []);

        var prepared = CombatEngineExecutor.PrepareEssenceAbility(
            definition.ActiveAbility, essence, definition, abilities.GetCatalog());
        var compiled = AbilityCompiler.CompileAbility(prepared);
        var effects = compiled.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers)
            ? triggers.SelectMany(x => x.Effects).Where(FastCombatEngine.IsImmediateChanneledEssenceComponent)
                .Select(x => x.Id).Distinct().ToArray()
            : [];
        return new(essence.Id, essence.EssenceDefinitionId, definition.DisplayName,
            prepared.Id, prepared.CooldownTicks, FastCombatEngine.HasEligibleChanneledEssenceComponent(compiled), effects);
    }
}
