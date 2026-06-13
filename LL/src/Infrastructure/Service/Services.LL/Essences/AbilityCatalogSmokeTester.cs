using Application.Interfaces.Services.LL.Essences;
using Domain.Interfaces.Combat;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

public sealed class AbilityCatalogSmokeTester : IAbilityCatalogSmokeTester
{
    private readonly IEssenceDefinitionRepository _definitions;
    private readonly IEssenceCombatAbilityFactory _factory;
    private readonly ICombatContext _combatContext;

    public AbilityCatalogSmokeTester(
        IEssenceDefinitionRepository definitions,
        IEssenceCombatAbilityFactory factory,
        ICombatContext combatContext)
    {
        _definitions = definitions;
        _factory = factory;
        _combatContext = combatContext;
    }

    public AbilityCatalogSmokeTestReport Run()
    {
        var failures = new List<AbilityCatalogSmokeTestFailure>();
        var definitions = _definitions.GetAll();
        var abilities = _definitions.GetAllAbilities();
        var scenariosChecked = 0;
        var compiledAbilities = 0;
        var combatSimulationsRun = 0;

        foreach (var ability in abilities)
        {
            scenariosChecked++;
            var role = GetRole(ability);
            var definition = FindReferencingDefinition(definitions, ability.Id, role);

            var scenario = CompileScenario(
                CreateCatalogRoleDefinition(definition, ability, role),
                CreatePlayerEssence(definition, evolved: false),
                definition.Id,
                ability.Id,
                role,
                "Catalog",
                failures);
            compiledAbilities += scenario.RuntimeAbilitiesCompiled;
            combatSimulationsRun += scenario.CombatSimulationsRun;
        }

        foreach (var scenario in CreateEvolutionScenarios(definitions))
        {
            scenariosChecked++;

            var result = CompileScenario(
                CreateRoleDefinition(scenario.Definition, scenario.Role),
                CreatePlayerEssence(scenario.Definition, evolved: true),
                scenario.Definition.Id,
                scenario.Ability.Id,
                scenario.Role,
                "Evolved",
                failures);
            compiledAbilities += result.RuntimeAbilitiesCompiled;
            combatSimulationsRun += result.CombatSimulationsRun;
        }

        return new AbilityCatalogSmokeTestReport(
            definitions.Count,
            abilities.Count,
            scenariosChecked,
            compiledAbilities,
            combatSimulationsRun,
            failures);
    }

    private AbilitySmokeScenarioResult CompileScenario(
        EssenceDefinition definition,
        PlayerEssence essence,
        string essenceDefinitionId,
        string abilityId,
        string role,
        string scenario,
        List<AbilityCatalogSmokeTestFailure> failures)
    {
        try
        {
            var compiled = _factory.CreateAbilities(definition, essence);

            if (compiled.Count != 1)
            {
                failures.Add(new AbilityCatalogSmokeTestFailure(
                    essenceDefinitionId,
                    abilityId,
                    role,
                    scenario,
                    $"Expected one runtime ability, but compiled {compiled.Count}."));
                return new AbilitySmokeScenarioResult(0, 0);
            }

            RunRepresentativeCombatSimulation(
                compiled[0].Ability,
                essenceDefinitionId,
                abilityId,
                role,
                scenario,
                failures);

            return new AbilitySmokeScenarioResult(compiled.Count, 1);
        }
        catch (Exception ex)
        {
            failures.Add(new AbilityCatalogSmokeTestFailure(
                essenceDefinitionId,
                abilityId,
                role,
                scenario,
                ex.Message));
            return new AbilitySmokeScenarioResult(0, 0);
        }
    }

    private void RunRepresentativeCombatSimulation(
        CombatAbilityInstance ability,
        string essenceDefinitionId,
        string abilityId,
        string role,
        string scenario,
        List<AbilityCatalogSmokeTestFailure> failures)
    {
        try
        {
            var source = CreateCombatEntity("source", "Smoke Test Source", [ability]);
            var target = CreateCombatEntity("target", "Smoke Test Target", []);

            var simulatedAbility = source.Abilities.First(x => x.Definition.Id.Equals(ability.Definition.Id, StringComparison.OrdinalIgnoreCase));
            simulatedAbility.RemainingTimeUntilUse = 0;
            source.NextBasicAttackIn = 3000;
            target.NextBasicAttackIn = 3000;

            var result = _combatContext.InstantiateAndRunCombat([source], [target]);
            if (result.Duration <= 0)
            {
                failures.Add(new AbilityCatalogSmokeTestFailure(
                    essenceDefinitionId,
                    abilityId,
                    role,
                    scenario,
                    "Representative combat simulation completed with no elapsed duration."));
            }
        }
        catch (Exception ex)
        {
            failures.Add(new AbilityCatalogSmokeTestFailure(
                essenceDefinitionId,
                abilityId,
                role,
                scenario,
                "Representative combat simulation failed: " + ex.Message));
        }
    }

    private static CombatEntity CreateCombatEntity(string id, string name, IReadOnlyList<CombatAbilityInstance> abilities)
    {
        var entity = new Character
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = 10,
            Abilities = [.. abilities]
        };

        FillCombatAttributes(entity.BaseCombatAttributes);
        FillCombatAttributes(entity.CombatAttributes);

        var combatEntity = new CombatEntity(entity)
        {
            Id = id,
            Name = name,
            Level = entity.Level
        };
        combatEntity.SyncCurrentHealthToMax();

        return combatEntity;
    }

    private static void FillCombatAttributes(IDictionary<AttributeType, float> attributes)
    {
        foreach (AttributeType attribute in Enum.GetValues<AttributeType>())
            attributes[attribute] = 0;

        attributes[AttributeType.Power] = 30;
        attributes[AttributeType.Fortitude] = 30;
        attributes[AttributeType.Precision] = 30;
        attributes[AttributeType.Spirit] = 30;
        attributes[AttributeType.MaxHealth] = 5000;
        attributes[AttributeType.WeaponDamage] = 5;
        attributes[AttributeType.CritDamage] = 100;
    }

    private static IEnumerable<AbilityEvolutionSmokeScenario> CreateEvolutionScenarios(IEnumerable<EssenceDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (!string.IsNullOrWhiteSpace(definition.ActiveAbility.Id)
                && definition.Evolution.ActiveAbilityModifiers.Count > 0)
            {
                yield return new AbilityEvolutionSmokeScenario(definition, "Active", definition.ActiveAbility);
            }

            if (!string.IsNullOrWhiteSpace(definition.PassiveAbility.Id)
                && definition.Evolution.PassiveAbilityModifiers.Count > 0)
            {
                yield return new AbilityEvolutionSmokeScenario(definition, "Passive", definition.PassiveAbility);
            }
        }
    }

    private static string GetRole(AbilityDefinition ability) =>
        ability.Kind.Equals(AbilityDefinitionKind.Passive, StringComparison.OrdinalIgnoreCase)
            ? "Passive"
            : "Active";

    private static EssenceDefinition FindReferencingDefinition(
        IEnumerable<EssenceDefinition> definitions,
        string abilityId,
        string role) =>
        definitions.FirstOrDefault(definition =>
            role.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? definition.ActiveAbilityId.Equals(abilityId, StringComparison.OrdinalIgnoreCase)
                : definition.PassiveAbilityId.Equals(abilityId, StringComparison.OrdinalIgnoreCase))
        ?? new EssenceDefinition
        {
            Id = $"catalog.{abilityId}",
            SourceMonsterId = "monster.catalog_smoke_test",
            Name = "Catalog Smoke Test",
            Evolution = new EssenceEvolutionDefinition()
        };

    private static EssenceDefinition CreateCatalogRoleDefinition(
        EssenceDefinition definition,
        AbilityDefinition ability,
        string role)
    {
        var isActive = role.Equals("Active", StringComparison.OrdinalIgnoreCase);

        return new EssenceDefinition
        {
            Id = definition.Id,
            SourceMonsterId = definition.SourceMonsterId,
            Name = definition.Name,
            Description = definition.Description,
            Rarity = definition.Rarity,
            Tags = [.. definition.Tags],
            AttributeBonuses = [.. definition.AttributeBonuses],
            ActiveAbilityId = isActive ? ability.Id : string.Empty,
            PassiveAbilityId = isActive ? string.Empty : ability.Id,
            ActiveAbility = isActive ? ability : new AbilityDefinition(),
            PassiveAbility = isActive ? new AbilityDefinition() : ability,
            Drop = definition.Drop,
            Ascension = definition.Ascension,
            Evolution = new EssenceEvolutionDefinition()
        };
    }

    private static EssenceDefinition CreateRoleDefinition(EssenceDefinition definition, string role)
    {
        var isActive = role.Equals("Active", StringComparison.OrdinalIgnoreCase);

        return new EssenceDefinition
        {
            Id = definition.Id,
            SourceMonsterId = definition.SourceMonsterId,
            Name = definition.Name,
            Description = definition.Description,
            Rarity = definition.Rarity,
            Tags = [.. definition.Tags],
            AttributeBonuses = [.. definition.AttributeBonuses],
            ActiveAbilityId = isActive ? definition.ActiveAbilityId : string.Empty,
            PassiveAbilityId = isActive ? string.Empty : definition.PassiveAbilityId,
            ActiveAbility = isActive ? definition.ActiveAbility : new AbilityDefinition(),
            PassiveAbility = isActive ? new AbilityDefinition() : definition.PassiveAbility,
            Drop = definition.Drop,
            Ascension = definition.Ascension,
            Evolution = new EssenceEvolutionDefinition
            {
                Id = definition.Evolution.Id,
                Name = definition.Evolution.Name,
                Description = definition.Evolution.Description,
                RequiredAscensionTier = definition.Evolution.RequiredAscensionTier,
                RequiredCatalystItemId = definition.Evolution.RequiredCatalystItemId,
                AddsTags = [.. definition.Evolution.AddsTags],
                AttributeModifierChanges = [.. definition.Evolution.AttributeModifierChanges],
                ActiveAbilityModifiers = isActive ? [.. definition.Evolution.ActiveAbilityModifiers] : [],
                PassiveAbilityModifiers = isActive ? [] : [.. definition.Evolution.PassiveAbilityModifiers]
            }
        };
    }

    private static PlayerEssence CreatePlayerEssence(EssenceDefinition definition, bool evolved) =>
        new()
        {
            Id = Guid.Empty,
            CharacterId = Guid.Empty,
            EssenceDefinitionId = definition.Id,
            Level = 1,
            AscensionTier = evolved ? Math.Max(1, definition.Evolution.RequiredAscensionTier) : 0,
            IsEvolved = evolved
        };

    private sealed record AbilityEvolutionSmokeScenario(EssenceDefinition Definition, string Role, AbilityDefinition Ability);
    private sealed record AbilitySmokeScenarioResult(int RuntimeAbilitiesCompiled, int CombatSimulationsRun);
}
