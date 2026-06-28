using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.CombatStyles.Models;
using Domain.Models.CombatStyles;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.Extensions.Logging;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleService : ICombatStyleService
{
    private const string DefaultStyleId = CombatStyleIds.Fighter;
    private readonly IPlayerCombatStyleRepository _combatStyles;
    private readonly ICombatStyleDefinitionProvider _definitions;
    private readonly ICombatStyleSwitchValidator _switchValidator;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IEssenceRepository _essences;
    private readonly ILogger<CombatStyleService> _logger;

    public CombatStyleService(
        IPlayerCombatStyleRepository combatStyles,
        ICombatStyleDefinitionProvider definitions,
        ICombatStyleSwitchValidator switchValidator,
        IEssenceDefinitionRepository essenceDefinitions,
        IEssenceRepository essences,
        ILogger<CombatStyleService> logger)
    {
        _combatStyles = combatStyles;
        _definitions = definitions;
        _switchValidator = switchValidator;
        _essenceDefinitions = essenceDefinitions;
        _essences = essences;
        _logger = logger;
    }

    public async Task<CombatStylesOverviewModel> GetOverviewAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var nodes = await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken);
        var active = progress.FirstOrDefault(x => x.IsActive);

        return new CombatStylesOverviewModel
        {
            ActiveStyleId = active?.StyleId,
            Styles =
            [
                .. _definitions.GetAll().Select(definition =>
                    MapStyle(
                        definition,
                        progress.Single(x => x.StyleId == definition.Id),
                        nodes.Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)).ToList()))
            ]
        };
    }

    public async Task<CombatStyleOperationResult> ActivateStyleAsync(
        Guid characterId,
        string styleId,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetById(styleId);
        if (definition is null)
            return CombatStyleOperationResult.Fail("Combat Style does not exist.");

        var validation = await _switchValidator.ValidateCanSwitchAsync(characterId, cancellationToken);
        if (!validation.CanSwitch)
        {
            _logger.LogInformation(
                "Combat Style switch blocked for character {CharacterId}: {Reason}",
                characterId,
                validation.Reason);
            return CombatStyleOperationResult.Fail(validation.Reason ?? "Cannot switch Combat Style right now.");
        }

        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var target = progress.Single(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));
        if (target.IsActive)
            return CombatStyleOperationResult.Success($"{definition.Name} Style activated.", definition.Id);

        var now = DateTimeOffset.UtcNow;
        await _combatStyles.DeactivateActiveStylesAsync(characterId, now, cancellationToken);

        foreach (var style in progress)
        {
            style.IsActive = ReferenceEquals(style, target);
            style.UpdatedAt = now;
        }

        _logger.LogInformation("Combat Style {StyleId} activated for character {CharacterId}", definition.Id, characterId);

        return CombatStyleOperationResult.Success($"{definition.Name} Style activated.", definition.Id);
    }

    public async Task<CombatStyleOperationResult<CombatStyleModel>> SelectFocusAsync(
        Guid characterId,
        string styleId,
        string focusId,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetById(styleId);
        if (definition is null)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Combat Style does not exist.");

        var focus = _definitions.GetFocus(styleId, focusId);
        if (focus is null)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Focus Path does not exist.");

        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var style = progress.Single(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));
        if (style.Level < focus.UnlockLevel)
            return CombatStyleOperationResult<CombatStyleModel>.Fail($"Focus Path requires Combat Style Level {focus.UnlockLevel}.");

        style.SelectedFocusId = focus.Id;
        style.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Combat Style focus {FocusId} selected for style {StyleId} and character {CharacterId}",
            focus.Id,
            definition.Id,
            characterId);

        var nodes = await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken);
        return CombatStyleOperationResult<CombatStyleModel>.Success(
            MapStyle(definition, style, nodes.Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)).ToList()),
            "Focus Path selected.");
    }

    public async Task<CombatStyleOperationResult<CombatStyleModel>> RankUpNodeAsync(
        Guid characterId,
        string styleId,
        string nodeId,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetById(styleId);
        if (definition is null)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Combat Style does not exist.");

        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var style = progress.Single(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));
        var styleNodes = (await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken))
            .Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var nodeDefinitions = definition.SkillTreeNodes.ToList();
        var node = nodeDefinitions.FirstOrDefault(x => x.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        if (node is null)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Skill tree node does not exist.");

        if (style.Level < node.RequiredLevel)
            return CombatStyleOperationResult<CombatStyleModel>.Fail($"Node requires Combat Style Level {node.RequiredLevel}.");

        var existing = styleNodes.FirstOrDefault(x => x.NodeId.Equals(node.Id, StringComparison.OrdinalIgnoreCase));
        if ((existing?.Rank ?? 0) >= node.MaxRank)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Node is already at maximum rank.");

        if (GetAvailableSkillPoints(style, styleNodes) <= 0)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("No Combat Style skill points available.");

        if (!IsNodeUnlocked(definition, style, styleNodes, node))
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Required node is not unlocked.");

        if (IsMajorNode(node)
            && existing is null
            && HasRankedMajorNodeInRow(nodeDefinitions, styleNodes, node.Row))
        {
            return CombatStyleOperationResult<CombatStyleModel>.Fail($"A major node is already selected in row {node.Row}.");
        }

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new PlayerCombatStyleNode
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                StyleId = definition.Id,
                NodeId = node.Id,
                Rank = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            styleNodes.Add(existing);
            await _combatStyles.AddNodeAsync(existing, cancellationToken);
        }
        else
        {
            existing.Rank++;
            existing.UpdatedAt = now;
        }

        style.SelectedFocusId = DetermineEffectiveFocusId(definition, style, styleNodes);
        style.UpdatedAt = now;

        return CombatStyleOperationResult<CombatStyleModel>.Success(
            MapStyle(definition, style, styleNodes),
            $"{node.Name} ranked up.");
    }

    public async Task<CombatStyleOperationResult<CombatStyleModel>> ResetSkillTreeAsync(
        Guid characterId,
        string styleId,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.GetById(styleId);
        if (definition is null)
            return CombatStyleOperationResult<CombatStyleModel>.Fail("Combat Style does not exist.");

        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var style = progress.Single(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));
        var nodes = (await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken))
            .Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _combatStyles.RemoveNodes(nodes);
        style.SelectedFocusId = null;
        style.UpdatedAt = DateTimeOffset.UtcNow;

        return CombatStyleOperationResult<CombatStyleModel>.Success(
            MapStyle(definition, style, []),
            "Combat Style skill tree reset.");
    }

    public async Task<CombatBuildPreviewModel> GetBuildPreviewAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var active = progress.First(x => x.IsActive);
        var definition = _definitions.GetById(active.StyleId) ?? _definitions.GetById(DefaultStyleId)!;
        var nodes = await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken);
        var focusId = DetermineEffectiveFocusId(
            definition,
            active,
            nodes.Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)).ToList());
        var focus = focusId is null ? null : _definitions.GetFocus(definition.Id, focusId);
        var tagScores = await CalculateTagScoresAsync(characterId, cancellationToken);
        var topTags = tagScores
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(6)
            .Select(x => new TagScoreModel { Tag = x.Key, Score = x.Value })
            .ToList();

        return new CombatBuildPreviewModel
        {
            ActiveStyleId = definition.Id,
            ActiveStyleName = definition.Name,
            SelectedFocusId = focus?.Id,
            SelectedFocusName = focus?.Name,
            BuildName = DetermineBuildName(definition, focus, tagScores),
            TopTags = topTags,
            RecommendedStats = [.. definition.RecommendedStats.Select(x => x.ToString())],
            Notes = CreateBuildNotes(definition, focus, tagScores)
        };
    }

    public async Task<CombatStyleSnapshot?> GetActiveSnapshotAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var active = progress.FirstOrDefault(x => x.IsActive);
        if (active is null)
            return null;

        var definition = _definitions.GetById(active.StyleId) ?? _definitions.GetById(DefaultStyleId);
        if (definition is null)
            return null;

        var nodes = await _combatStyles.GetNodesByCharacterIdAsync(characterId, cancellationToken);
        var focusId = DetermineEffectiveFocusId(
            definition,
            active,
            nodes.Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)).ToList());
        var focus = focusId is null ? null : _definitions.GetFocus(definition.Id, focusId);
        return new CombatStyleSnapshot(
            definition.Id,
            definition.Name,
            active.Level,
            active.Experience,
            focus?.Id,
            focus?.Name,
            nodes
                .Where(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase) && x.Rank > 0)
                .GroupBy(x => x.NodeId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Sum(node => node.Rank), StringComparer.OrdinalIgnoreCase));
    }

    public async Task GrantExperienceAsync(Guid characterId, long amount, string source, CancellationToken cancellationToken)
    {
        if (amount <= 0)
            return;

        var progress = await EnsureProgressAsync(characterId, cancellationToken);
        var active = progress.First(x => x.IsActive);
        var definition = _definitions.GetById(active.StyleId);
        if (definition is null || active.Level >= definition.MaxLevel)
            return;

        active.Experience += amount;
        while (active.Level < definition.MaxLevel && active.Experience >= ExperienceForNextLevel(active.Level))
            active.Level++;

        active.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Granted {Amount} Combat Style XP from {Source} to {StyleId} for character {CharacterId}",
            amount,
            source,
            active.StyleId,
            characterId);
    }

    private async Task<List<PlayerCombatStyle>> EnsureProgressAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var definitions = _definitions.GetAll().ToList();
        var progress = await _combatStyles.GetByCharacterIdAsync(characterId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var definition in definitions)
        {
            if (progress.Any(x => x.StyleId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)))
                continue;

            var style = new PlayerCombatStyle
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                StyleId = definition.Id,
                Level = 1,
                IsActive = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            progress.Add(style);
            await _combatStyles.AddAsync(style, cancellationToken);
        }

        if (progress.All(x => !x.IsActive))
        {
            var defaultStyle = progress.FirstOrDefault(x => x.StyleId.Equals(DefaultStyleId, StringComparison.OrdinalIgnoreCase))
                ?? progress.First();
            defaultStyle.IsActive = true;
            defaultStyle.UpdatedAt = now;
        }

        return progress;
    }

    private CombatStyleModel MapStyle(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes)
    {
        var selectedFocusId = DetermineEffectiveFocusId(definition, progress, nodes);
        var spent = GetSpentSkillPoints(nodes);
        var earned = GetEarnedSkillPoints(progress);
        return new()
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            ResourceId = definition.ResourceId,
            CoreMechanic = definition.CoreMechanic,
            Level = progress.Level,
            Experience = progress.Experience,
            ExperienceForCurrentLevel = ExperienceForLevel(progress.Level),
            ExperienceForNextLevel = ExperienceForNextLevel(progress.Level),
            IsActive = progress.IsActive,
            SelectedFocusId = selectedFocusId,
            SkillPointsEarned = earned,
            SkillPointsSpent = spent,
            SkillPointsAvailable = Math.Max(0, earned - spent),
            RecommendedTags = definition.RecommendedTags,
            RecommendedStats = [.. definition.RecommendedStats.Select(x => x.ToString())],
            Focuses = [.. definition.Focuses.Select(focus => new CombatStyleFocusModel
            {
                Id = focus.Id,
                Name = focus.Name,
                Description = focus.Description,
                UnlockLevel = focus.UnlockLevel,
                IsUnlocked = progress.Level >= focus.UnlockLevel,
                IsSelected = focus.Id.Equals(selectedFocusId, StringComparison.OrdinalIgnoreCase),
                RecommendedTags = focus.RecommendedTags,
                RecommendedStats = [.. focus.RecommendedStats.Select(x => x.ToString())]
            })],
            SkillTree = CreateSkillTreeDto(definition, progress, nodes),
            RuleSummaries = [.. definition.Rules.Select(rule => new CombatStyleRuleSummaryModel
            {
                Id = rule.Id,
                Text = FormatRuleSummary(rule)
            })]
        };
    }

    private static CombatStyleSkillTreeModel CreateSkillTreeDto(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes)
    {
        var earned = GetEarnedSkillPoints(progress);
        var spent = GetSpentSkillPoints(nodes);
        var available = Math.Max(0, earned - spent);

        return new CombatStyleSkillTreeModel
        {
            Branches =
            [
                .. (UsesRowLaneTree(definition)
                    ? CreateRowLaneBranches(definition, progress, nodes, available)
                    : CreateFocusBranches(definition, progress, nodes, available))
            ]
        };
    }

    private static IEnumerable<CombatStyleSkillTreeBranchModel> CreateFocusBranches(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes,
        int available)
    {
        var nodeDefinitions = definition.SkillTreeNodes.ToList();
        return definition.Focuses.Select(focus =>
        {
            var branchNodes = nodeDefinitions
                .Where(x => x.BranchId.Equals(focus.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Y)
                .ThenBy(x => x.X)
                .ToList();

            return new CombatStyleSkillTreeBranchModel
            {
                Id = focus.Id,
                Name = focus.Name,
                Description = focus.Description,
                RecommendedTags = focus.RecommendedTags,
                RecommendedStats = [.. focus.RecommendedStats.Select(x => x.ToString())],
                PointsSpent = nodes
                    .Where(node => branchNodes.Any(def => def.Id.Equals(node.NodeId, StringComparison.OrdinalIgnoreCase)))
                    .Sum(x => x.Rank),
                Nodes =
                [
                    .. branchNodes.Select(node =>
                    {
                        var rank = GetNodeRank(nodes, node.Id);
                        var unlocked = IsNodeUnlocked(definition, progress, nodes, node);
                        return MapNode(node, rank, unlocked, unlocked && rank < node.MaxRank && available > 0);
                    })
                ]
            };
        });
    }

    private static IEnumerable<CombatStyleSkillTreeBranchModel> CreateRowLaneBranches(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes,
        int available)
    {
        var lanes = new[]
        {
            CombatStyleNodeLanes.Left,
            CombatStyleNodeLanes.Middle,
            CombatStyleNodeLanes.Right
        };
        var nodeDefinitions = definition.SkillTreeNodes.ToList();

        foreach (var lane in lanes)
        {
            var branchNodes = nodeDefinitions
                .Where(x => x.Lane.Equals(lane, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Row)
                .ThenBy(x => x.Y)
                .ThenBy(x => x.X)
                .ToList();

            yield return new CombatStyleSkillTreeBranchModel
            {
                Id = lane.ToLowerInvariant(),
                Name = lane,
                Description = $"{definition.Name} {lane.ToLowerInvariant()} lane.",
                RecommendedTags = [lane],
                RecommendedStats = [.. definition.RecommendedStats.Select(x => x.ToString())],
                PointsSpent = nodes
                    .Where(node => branchNodes.Any(def => def.Id.Equals(node.NodeId, StringComparison.OrdinalIgnoreCase)))
                    .Sum(x => x.Rank),
                Nodes =
                [
                    .. branchNodes.Select(node =>
                    {
                        var rank = GetNodeRank(nodes, node.Id);
                        var unlocked = IsNodeUnlocked(definition, progress, nodes, node);
                        var majorRowBlocked = IsMajorNode(node)
                            && rank <= 0
                            && HasRankedMajorNodeInRow(nodeDefinitions, nodes, node.Row);
                        return MapNode(
                            node,
                            rank,
                            unlocked,
                            unlocked && !majorRowBlocked && rank < node.MaxRank && available > 0);
                    })
                ]
            };
        }
    }

    private static CombatStyleSkillTreeNodeModel MapNode(
        CombatStyleTreeNodeDefinition node,
        int rank,
        bool unlocked,
        bool canRankUp) =>
        new()
        {
            Id = node.Id,
            BranchId = node.BranchId,
            Name = node.Name,
            Description = node.Description,
            Rank = rank,
            MaxRank = node.MaxRank,
            RequiredLevel = node.RequiredLevel,
            RequiredNodeId = node.RequiredNodeId,
            X = node.X,
            Y = node.Y,
            IsUnlocked = unlocked,
            CanRankUp = canRankUp,
            Tags = node.Tags,
            Effects = node.Effects,
            Row = node.Row,
            Lane = node.Lane,
            NodeType = node.NodeType,
            MutatorKind = node.MutatorKind,
            MutatorGroups = node.MutatorGroups,
            Tooltip = new CombatStyleNodeTooltipModel
            {
                Affects = node.Tooltip.Affects,
                Tradeoffs = node.Tooltip.Tradeoffs,
                DoesNotAffect = node.Tooltip.DoesNotAffect
            }
        };

    private static string? DetermineEffectiveFocusId(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes)
    {
        if (UsesRowLaneTree(definition))
        {
            var rowThreeMajor = definition.SkillTreeNodes
                .Where(node => IsMajorNode(node) && node.Row == 3 && GetNodeRank(nodes, node.Id) > 0)
                .OrderBy(node => node.X)
                .FirstOrDefault();

            return rowThreeMajor?.Id ?? progress.SelectedFocusId;
        }

        var nodeDefinitions = definition.SkillTreeNodes;
        var branchScores = definition.Focuses
            .Select(focus => new
            {
                focus.Id,
                Points = nodes
                    .Where(node => nodeDefinitions.Any(def =>
                        def.BranchId.Equals(focus.Id, StringComparison.OrdinalIgnoreCase) &&
                        def.CountsTowardFocus &&
                        def.Id.Equals(node.NodeId, StringComparison.OrdinalIgnoreCase)))
                    .Sum(x => x.Rank)
            })
            .Where(x => x.Points > 0)
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.Id.Equals(progress.SelectedFocusId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => definition.Focuses.ToList().FindIndex(focus => focus.Id.Equals(x.Id, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();

        return branchScores?.Id ?? progress.SelectedFocusId;
    }

    private static bool IsNodeUnlocked(
        CombatStyleDefinition definition,
        PlayerCombatStyle progress,
        IReadOnlyList<PlayerCombatStyleNode> nodes,
        CombatStyleTreeNodeDefinition node)
    {
        if (progress.Level < node.RequiredLevel)
            return false;

        if (!UsesRowLaneTree(definition))
        {
            var requiredRank = node.RequiredNodeId is null
                ? 1
                : GetNodeRank(nodes, node.RequiredNodeId);
            return requiredRank > 0;
        }

        if (node.Row <= 1)
            return true;

        var previousMajor = definition.SkillTreeNodes.FirstOrDefault(candidate =>
            IsMajorNode(candidate)
            && candidate.Row == node.Row - 1
            && GetNodeRank(nodes, candidate.Id) > 0);

        if (previousMajor is null)
            return false;

        return !IsMajorNode(node) || CanLaneUnlock(previousMajor.Lane, node.Lane);
    }

    private static bool CanLaneUnlock(string sourceLane, string targetLane) =>
        sourceLane.Equals(CombatStyleNodeLanes.Middle, StringComparison.OrdinalIgnoreCase)
        || sourceLane.Equals(targetLane, StringComparison.OrdinalIgnoreCase)
        || (sourceLane.Equals(CombatStyleNodeLanes.Left, StringComparison.OrdinalIgnoreCase)
            && targetLane.Equals(CombatStyleNodeLanes.Middle, StringComparison.OrdinalIgnoreCase))
        || (sourceLane.Equals(CombatStyleNodeLanes.Right, StringComparison.OrdinalIgnoreCase)
            && targetLane.Equals(CombatStyleNodeLanes.Middle, StringComparison.OrdinalIgnoreCase));

    private static bool UsesRowLaneTree(CombatStyleDefinition definition) =>
        definition.SkillTreeNodes.Any(x => x.Row > 0);

    private static bool IsMajorNode(CombatStyleTreeNodeDefinition node) =>
        node.NodeType.Equals(CombatStyleNodeTypes.Major, StringComparison.OrdinalIgnoreCase);

    private static bool HasRankedMajorNodeInRow(
        IReadOnlyList<CombatStyleTreeNodeDefinition> nodeDefinitions,
        IReadOnlyList<PlayerCombatStyleNode> nodes,
        int row) =>
        row > 0
        && nodeDefinitions
            .Where(node => IsMajorNode(node) && node.Row == row)
            .Any(node => GetNodeRank(nodes, node.Id) > 0);

    private static int GetNodeRank(IReadOnlyList<PlayerCombatStyleNode> nodes, string nodeId) =>
        nodes.FirstOrDefault(x => x.NodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase))?.Rank ?? 0;

    private static int GetEarnedSkillPoints(PlayerCombatStyle style) =>
        Math.Max(1, style.Level);

    private static int GetSpentSkillPoints(IEnumerable<PlayerCombatStyleNode> nodes) =>
        nodes.Sum(x => x.Rank);

    private static int GetAvailableSkillPoints(
        PlayerCombatStyle style,
        IReadOnlyList<PlayerCombatStyleNode> nodes) =>
        Math.Max(0, GetEarnedSkillPoints(style) - GetSpentSkillPoints(nodes));

    private async Task<Dictionary<string, int>> CalculateTagScoresAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var loadout = await _essences.GetActiveLoadoutAsync(characterId, cancellationToken);

        if (loadout is null)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var essence in loadout.Slots.Select(x => x.PlayerEssence).OfType<PlayerEssence>())
        {
            var definition = _essenceDefinitions.GetById(essence.EssenceDefinitionId);
            if (definition is null)
                continue;

            AddTags(scores, definition.Tags, 1);
            AddTags(scores, definition.ActiveAbility.Tags, 2);
            AddTags(scores, definition.PassiveAbility.Tags, 2);
        }

        return scores;
    }

    private static void AddTags(Dictionary<string, int> scores, IEnumerable<string> tags, int amount)
    {
        foreach (var tag in tags.Where(x => !string.IsNullOrWhiteSpace(x)))
            scores[tag] = scores.GetValueOrDefault(tag) + amount;
    }

    private static string DetermineBuildName(
        CombatStyleDefinition style,
        CombatStyleFocusDefinition? focus,
        IReadOnlyDictionary<string, int> tags)
    {
        var focusId = focus?.Id ?? string.Empty;
        var summon = Score(tags, "Summon");
        var barrier = Score(tags, "Barrier");
        var tank = Score(tags, "Tank");
        var melee = Score(tags, "Melee");
        var ranged = Score(tags, "Ranged");
        var magic = Score(tags, "Magic") + Score(tags, "Spell");
        var curse = Score(tags, "Curse") + Score(tags, "DoT");
        var holy = Score(tags, "Holy");
        var healing = Score(tags, "Healing") + Score(tags, "Heal");
        var control = Score(tags, "Control") + Score(tags, "Debuff");
        var dodge = Score(tags, "Dodge");

        return (style.Id, focusId) switch
        {
            ("defensive", "commander") when summon >= 4 => "Tanky Summoner",
            ("defensive", "bulwark") when barrier + tank >= 4 => "Bulwark Guardian",
            ("defensive", "counterguard") => "Counterguard",
            ("caster", "spellblade") when melee >= 3 => "Magic Swordsman",
            ("caster", "occultist") when curse >= 3 => "Hexcaster",
            ("caster", "arcanist") when magic >= 3 => "Arcanist",
            ("fighter", "duelist") => "Duelist",
            ("fighter", "berserker") => "Berserker",
            ("summoner", "horde") => "Swarmcaller",
            ("summoner", "champion") => "Soul Champion",
            ("summoner", "ritualist") when curse >= 3 => "Necromancer",
            ("summoner", "ritualist") when holy >= 3 => "Divine Host",
            ("swift", "flurry") when melee + ranged >= 3 => "Blade Dancer",
            ("swift", "evasion") when dodge + tank >= 3 => "Mist Walker",
            ("swift", "tempo") => "Tempo Striker",
            ("marksman", "sniper") => "Sharpshooter",
            ("marksman", "volley") when ranged >= 3 => "Volley Captain",
            ("marksman", "trapper") => "Trap Ranger",
            ("support", "healer") when healing >= 3 => "Restoration Adept",
            ("support", "warden") when barrier + tank >= 3 => "Aegis Warden",
            ("support", "chaplain") when holy >= 3 => "Battle Chaplain",
            ("controller", "hexer") when curse >= 3 => "Hex Controller",
            ("controller", "tactician") when control >= 3 => "Field Tactician",
            ("controller", "frostbinder") => "Frostbinder",
            _ => $"{style.Name} Adventurer"
        };
    }

    private static List<string> CreateBuildNotes(
        CombatStyleDefinition style,
        CombatStyleFocusDefinition? focus,
        IReadOnlyDictionary<string, int> tags)
    {
        var notes = new List<string>
        {
            focus is null
                ? $"{style.Name} Style is active. Invest past a branch root to form an effective focus."
                : $"{focus.Name} Focus is active through skill tree investment."
        };

        var matchingTags = style.RecommendedTags.Where(tag => Score(tags, tag) > 0).ToList();
        if (matchingTags.Count > 0)
            notes.Add($"Current loadout matches {string.Join(", ", matchingTags)} style synergies.");

        return notes;
    }

    private static int Score(IReadOnlyDictionary<string, int> tags, string tag) =>
        tags.TryGetValue(tag, out var score) ? score : 0;

    private static string FormatRuleSummary(CombatStyleRuleDefinition rule) =>
        rule.Operation switch
        {
            AddDamageReductionOperation op => $"{op.Percent:P0} incoming damage reduction.",
            GainStyleResourceOperation op => $"Gain {op.Amount:0.#} {op.ResourceId} when triggered.",
            ModifyEffectAmountOperation op => $"{op.AdditivePercent:P0} effect amount.",
            AddBonusDamageFromStatOperation op => $"Adds {op.Coefficient:P0} of {op.Stat} as {op.DamageType} bonus damage.",
            _ => rule.Id
        };

    private static long ExperienceForLevel(int level) =>
        level <= 1 ? 0 : Enumerable.Range(1, level - 1).Sum(ExperienceForNextLevel);

    private static long ExperienceForNextLevel(int level) =>
        level >= 50 ? ExperienceForLevel(50) : 100L * level * level;

}
