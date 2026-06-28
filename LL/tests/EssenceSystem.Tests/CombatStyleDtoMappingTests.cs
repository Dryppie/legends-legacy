using Application.Common.Mappings;
using Application.Interfaces.Services.LL.CombatStyles;
using Application.UseCases.CombatStyles.Dtos;
using Application.UseCases.CombatStyles.Models;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CombatStyleDtoMappingTests
{
    [Fact]
    public void CombatStyleDtoProfiles_MapServiceModels()
    {
        var mapper = CreateMapper();
        var style = new CombatStyleModel
        {
            Id = "fighter",
            Name = "Fighter",
            Description = "Builds Momentum.",
            ResourceId = "momentum",
            CoreMechanic = "Gain Momentum from direct damage.",
            Level = 10,
            Experience = 250,
            ExperienceForCurrentLevel = 200,
            ExperienceForNextLevel = 300,
            IsActive = true,
            SelectedFocusId = "duelist",
            SkillPointsEarned = 3,
            SkillPointsSpent = 2,
            SkillPointsAvailable = 1,
            RecommendedTags = ["Melee"],
            RecommendedStats = ["Power"],
            Focuses =
            [
                new CombatStyleFocusModel
                {
                    Id = "duelist",
                    Name = "Duelist",
                    Description = "Single target pressure.",
                    UnlockLevel = 10,
                    IsUnlocked = true,
                    IsSelected = true,
                    RecommendedTags = ["Melee"],
                    RecommendedStats = ["Precision"]
                }
            ],
            SkillTree = new CombatStyleSkillTreeModel
            {
                Branches =
                [
                    new CombatStyleSkillTreeBranchModel
                    {
                        Id = "duelist",
                        Name = "Duelist",
                        Description = "Single target pressure.",
                        PointsSpent = 2,
                        RecommendedTags = ["Melee"],
                        RecommendedStats = ["Precision"],
                        Nodes =
                        [
                            new CombatStyleSkillTreeNodeModel
                            {
                                Id = "duelist-technique",
                                BranchId = "duelist",
                                Name = "Duelist Technique",
                                Description = "Improves current-target active damage.",
                                Rank = 1,
                                MaxRank = 3,
                                RequiredLevel = 10,
                                RequiredNodeId = "duelist-path",
                                X = 0,
                                Y = 1,
                                IsUnlocked = true,
                                CanRankUp = true,
                                Tags = ["Melee"],
                                Effects = ["Each rank gives active damage against the current target +2% effect amount."],
                                Row = 2,
                                Lane = "Middle",
                                NodeType = "Major",
                                MutatorKind = "Converter",
                                MutatorGroups = ["DamageTypeConversion"],
                                Tooltip = new CombatStyleNodeTooltipModel
                                {
                                    Affects = ["Physical melee abilities."],
                                    Changes = ["Damage becomes magical."],
                                    Tradeoffs = ["Resource cost +5%."],
                                    DoesNotAffect = ["True damage."]
                                }
                            }
                        ]
                    }
                ]
            },
            RuleSummaries = [new CombatStyleRuleSummaryModel { Id = "fighter_base", Text = "Direct damage builds Momentum." }]
        };
        var overview = new CombatStylesOverviewModel
        {
            ActiveStyleId = "fighter",
            Styles = [style]
        };
        var preview = new CombatBuildPreviewModel
        {
            ActiveStyleId = "fighter",
            ActiveStyleName = "Fighter",
            SelectedFocusId = "duelist",
            SelectedFocusName = "Duelist",
            BuildName = "Duelist Fighter",
            TopTags = [new TagScoreModel { Tag = "Melee", Score = 4 }],
            RecommendedStats = ["Power"],
            Notes = ["Fighter Style is active."]
        };

        var overviewDto = mapper.Map<CombatStylesOverviewDto>(overview);
        var mutationDto = mapper.Map<CombatStyleMutationResponseDto>(
            CombatStyleOperationResult<CombatStyleModel>.Success(style, "Ranked up."));
        var activationDto = mapper.Map<ActivateCombatStyleResponseDto>(
            CombatStyleOperationResult.Success("Fighter Style activated.", "fighter"));
        var previewDto = mapper.Map<CombatBuildPreviewDto>(preview);
        var nodeDto = overviewDto.Styles.Single().SkillTree.Branches.Single().Nodes.Single();

        Assert.Equal("fighter", overviewDto.ActiveStyleId);
        Assert.Equal("duelist-technique", nodeDto.Id);
        Assert.Equal("Middle", nodeDto.Lane);
        Assert.Equal("Major", nodeDto.NodeType);
        Assert.Equal("Converter", nodeDto.MutatorKind);
        Assert.Equal("DamageTypeConversion", nodeDto.MutatorGroups.Single());
        Assert.Equal("Damage becomes magical.", nodeDto.Tooltip.Changes.Single());
        Assert.Equal("Direct damage builds Momentum.", overviewDto.Styles.Single().RuleSummaries.Single().Text);
        Assert.True(mutationDto.Success);
        Assert.Equal("fighter", mutationDto.Style?.Id);
        Assert.Equal("fighter", activationDto.ActiveStyleId);
        Assert.Equal("Melee", previewDto.TopTags.Single().Tag);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
