using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class CombatRatingServiceTests
{
    [Fact]
    public void Calculate_separates_equipment_essence_attributes_and_ability_power()
    {
        var resolver = new FixedLoadoutResolver(
            [
                new EssenceAttributeModifier(AttributeType.Power, 5, ModifierType.Flat),
                new EssenceAttributeModifier(AttributeType.MaxHealth, 10, ModifierType.Additive)
            ],
            abilityCombatRating: 150);
        var service = new CombatRatingService(resolver);
        var essence = new PlayerEssence { EssenceDefinitionId = "essence.test", Level = 1 };

        var result = service.Calculate(
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 100,
                [AttributeType.Power] = 10
            },
            [],
            [essence]);

        Assert.Equal(260, result.Breakdown.BaseAndEquipment);
        Assert.Equal(58, result.Breakdown.EssenceAttributes);
        Assert.Equal(150, result.Breakdown.EssenceAbilities);
        Assert.Equal(468, result.Breakdown.Total);
        Assert.Equal(110, result.Attributes[AttributeType.MaxHealth]);
        Assert.Equal(15, result.Attributes[AttributeType.Power]);
    }

    private sealed class FixedLoadoutResolver(
        IReadOnlyList<AttributeModifierBase> modifiers,
        int abilityCombatRating) : IEssenceCombatLoadoutResolver
    {
        public Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(Resolve(characterId, []));

        public EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences) =>
            new(
                characterId,
                equippedEssences.ToList(),
                modifiers,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                abilityCombatRating);
    }
}
