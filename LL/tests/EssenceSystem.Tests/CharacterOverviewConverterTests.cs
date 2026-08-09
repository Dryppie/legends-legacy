using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Characters.Dtos;
using Domain.Helpers.Constants;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.Definitions;
using Domain.Models.Professions;

namespace EssenceSystem.Tests;

public sealed class CharacterOverviewConverterTests
{
    [Fact]
    public void Convert_UsesHighestCraftingProfessionForLevelAndExperience()
    {
        var character = new Character
        {
            Professions =
            [
                new Profession
                {
                    ProfessionType = ProfessionType.Mining,
                    Level = 20,
                    Experience = 99
                },
                new Profession
                {
                    ProfessionType = ProfessionType.Crafting,
                    Level = 4,
                    Experience = 75
                },
                new Profession
                {
                    ProfessionType = (ProfessionType)3,
                    Level = 5,
                    Experience = 42
                }
            ]
        };

        var result = new CharacterOverviewConverter(new EmptyEssenceDefinitions())
            .Convert(character, null!, null!);

        Assert.Equal(5, result.CraftingLevel);
        Assert.Equal(42, result.CraftingExperience);
        Assert.Equal(
            EntityLevelConstants.XP_REQUIRED(5),
            result.CraftingExperienceUntilNextLevel);
    }

    [Fact]
    public void Convert_DefaultsToLevelOneCraftingProgress()
    {
        var result = new CharacterOverviewConverter(new EmptyEssenceDefinitions())
            .Convert(new Character(), null!, null!);

        Assert.Equal(1, result.CraftingLevel);
        Assert.Equal(0, result.CraftingExperience);
        Assert.Equal(
            EntityLevelConstants.XP_REQUIRED(1),
            result.CraftingExperienceUntilNextLevel);
    }

    private sealed class EmptyEssenceDefinitions : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => [];

        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];

        public EssenceDefinition? GetById(string essenceDefinitionId) => null;

        public AbilitySpec? GetAbilityById(string abilityId) => null;
    }
}
