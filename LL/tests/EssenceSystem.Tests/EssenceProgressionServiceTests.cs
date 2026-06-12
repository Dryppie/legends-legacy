using Application.Interfaces.Services.LL.Essences;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Services.LL.Essences;
using AuthoredAbilityDefinition = Domain.Models.AbilityDefinitions.AbilityDefinition;

namespace EssenceSystem.Tests;

public sealed class EssenceProgressionServiceTests
{
    [Fact]
    public void GrantXp_does_not_bank_xp_past_current_tier_cap()
    {
        var service = new EssenceProgressionService(new FakeDefinitionRepository());
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 9,
            CurrentXp = 0,
            AscensionTier = 0
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), 10_000);

        Assert.Equal(10, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.True(result.ReachedTierCap);
        Assert.Equal(1, result.LevelsGained);
        Assert.True(result.XpGained < 10_000);
    }

    [Fact]
    public void GrantXp_returns_no_gain_when_already_at_current_tier_cap()
    {
        var service = new EssenceProgressionService(new FakeDefinitionRepository());
        var essence = new PlayerEssence
        {
            EssenceDefinitionId = "essence.test",
            Level = 10,
            CurrentXp = 0,
            AscensionTier = 0
        };

        var result = service.GrantXp(essence, EssenceDefinitionValidatorTests.ValidDefinition(), 500);

        Assert.Equal(10, essence.Level);
        Assert.Equal(0, essence.CurrentXp);
        Assert.Equal(0, result.XpGained);
        Assert.True(result.ReachedTierCap);
    }

    private sealed class FakeDefinitionRepository : IEssenceDefinitionRepository
    {
        private readonly IReadOnlyDictionary<string, EssenceProgressionTemplate> _templates =
            new Dictionary<string, EssenceProgressionTemplate>
            {
                ["hybrid"] = new()
                {
                    BaseXpPerLevel = 100,
                    XpGrowth = 0
                }
            };

        public IReadOnlyList<EssenceDefinition> GetAll() => [EssenceDefinitionValidatorTests.ValidDefinition()];

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            GetAll().FirstOrDefault(x => x.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public EssenceDefinition? GetByMonsterId(string monsterId) => null;

        public IReadOnlyDictionary<string, EssenceProgressionTemplate> GetProgressionTemplates() => _templates;

        public AuthoredAbilityDefinition? GetAbilityById(string abilityId) =>
            GetAll().SelectMany(x => new[] { x.ActiveAbility, x.PassiveAbility })
                .FirstOrDefault(x => x.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
    }
}
