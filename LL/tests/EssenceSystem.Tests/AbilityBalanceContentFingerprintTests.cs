using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Domain.Models.Items;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class AbilityBalanceContentFingerprintTests
{
    [Fact]
    public void Essence_rarity_changes_invalidate_the_content_fingerprint()
    {
        var catalog = new StubCatalogProvider();
        var common = new StubEssenceDefinitions(Rarity.Common);
        var rare = new StubEssenceDefinitions(Rarity.Rare);

        var first = AbilityBalanceContentFingerprint.Create(catalog, common);
        var changed = AbilityBalanceContentFingerprint.Create(catalog, rare);

        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void Equivalent_content_produces_a_stable_fingerprint()
    {
        var catalog = new StubCatalogProvider();

        var first = AbilityBalanceContentFingerprint.Create(
            catalog,
            new StubEssenceDefinitions(Rarity.Common));
        var replay = AbilityBalanceContentFingerprint.Create(
            catalog,
            new StubEssenceDefinitions(Rarity.Common));

        Assert.Equal(first, replay);
    }

    private sealed class StubCatalogProvider : IAbilityCatalogProvider
    {
        private readonly AbilityCatalog _catalog = new([], [], [], new Dictionary<string, string>());

        public AbilityCatalog GetCatalog() => _catalog;
    }

    private sealed class StubEssenceDefinitions(Rarity rarity) : IEssenceDefinitionRepository
    {
        private readonly IReadOnlyList<EssenceDefinition> _definitions =
        [
            new()
            {
                Id = "essence.test",
                SourceMonsterId = "monster.test",
                Name = "Test",
                Rarity = rarity,
                ActiveAbilityId = "ability.test.active",
                PassiveAbilityId = "ability.test.passive"
            }
        ];

        public IReadOnlyList<EssenceDefinition> GetAll() => _definitions;
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];
        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            _definitions.SingleOrDefault(definition => definition.Id == essenceDefinitionId);
        public AbilitySpec? GetAbilityById(string abilityId) => null;
    }
}
