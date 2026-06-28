namespace Application.UseCases.CombatStyles.Models;

public sealed class CombatStylesOverviewModel
{
    public string? ActiveStyleId { get; set; }
    public IReadOnlyList<CombatStyleModel> Styles { get; set; } = [];
}

public sealed class CombatStyleModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string CoreMechanic { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long ExperienceForCurrentLevel { get; set; }
    public long ExperienceForNextLevel { get; set; }
    public bool IsActive { get; set; }
    public string? SelectedFocusId { get; set; }
    public int SkillPointsEarned { get; set; }
    public int SkillPointsSpent { get; set; }
    public int SkillPointsAvailable { get; set; }
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public IReadOnlyList<CombatStyleFocusModel> Focuses { get; set; } = [];
    public CombatStyleSkillTreeModel SkillTree { get; set; } = new();
    public IReadOnlyList<CombatStyleRuleSummaryModel> RuleSummaries { get; set; } = [];
}

public sealed class CombatStyleFocusModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UnlockLevel { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsSelected { get; set; }
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
}

public sealed class CombatStyleRuleSummaryModel
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class CombatStyleSkillTreeModel
{
    public IReadOnlyList<CombatStyleSkillTreeBranchModel> Branches { get; set; } = [];
}

public sealed class CombatStyleSkillTreeBranchModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> RecommendedTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public int PointsSpent { get; set; }
    public IReadOnlyList<CombatStyleSkillTreeNodeModel> Nodes { get; set; } = [];
}

public sealed class CombatStyleSkillTreeNodeModel
{
    public string Id { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int MaxRank { get; set; }
    public int RequiredLevel { get; set; }
    public string? RequiredNodeId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsUnlocked { get; set; }
    public bool CanRankUp { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = [];
    public IReadOnlyList<string> Effects { get; set; } = [];
    public int Row { get; set; }
    public string Lane { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? MutatorKind { get; set; }
    public IReadOnlyList<string> MutatorGroups { get; set; } = [];
    public CombatStyleNodeTooltipModel Tooltip { get; set; } = new();
}

public sealed class CombatStyleNodeTooltipModel
{
    public IReadOnlyList<string> Affects { get; set; } = [];
    public IReadOnlyList<string> Changes { get; set; } = [];
    public IReadOnlyList<string> Tradeoffs { get; set; } = [];
    public IReadOnlyList<string> DoesNotAffect { get; set; } = [];
}

public sealed class CombatBuildPreviewModel
{
    public string ActiveStyleId { get; set; } = string.Empty;
    public string ActiveStyleName { get; set; } = string.Empty;
    public string? SelectedFocusId { get; set; }
    public string? SelectedFocusName { get; set; }
    public string BuildName { get; set; } = string.Empty;
    public IReadOnlyList<TagScoreModel> TopTags { get; set; } = [];
    public IReadOnlyList<string> RecommendedStats { get; set; } = [];
    public IReadOnlyList<string> Notes { get; set; } = [];
}

public sealed class TagScoreModel
{
    public string Tag { get; set; } = string.Empty;
    public int Score { get; set; }
}
