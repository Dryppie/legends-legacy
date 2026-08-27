using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Snapshots;
using Domain.Models.Essences;
using Persistence.LL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Essences;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class CanonicalEquipmentBuildFactoryTests
{
    [Fact]
    public void Late_tower_requirement_curve_is_monotonic_and_matches_both_anchors()
    {
        var services = CreateServices();
        var roles = CanonicalCooperativeRosterCatalog.CreateParty(10);
        var requirements = Enumerable.Range(11, 10)
            .Select(WorldTowerEquipmentRequirementCurve.Get)
            .ToArray();
        var ratings = requirements.Select(requirement =>
            {
                var rung = services.Factory.GetProgressionLadder().Single(candidate =>
                    candidate.Tier == requirement.Tier
                    && candidate.Rarity == requirement.Rarity
                    && candidate.Quality == requirement.Quality);
                return roles.Average(role => CombatRatingDisplay.FromRaw(
                    services.Factory.CreateBuild(
                        role.Role,
                        rung,
                        requirement.EssenceCount).Rating.Overall));
            })
            .ToArray();

        Assert.Equal(
            new WorldTowerEquipmentRequirement(11, 2, Rarity.Epic, ItemQuality.Fine, 7),
            requirements[0]);
        Assert.Equal(
            new WorldTowerEquipmentRequirement(
                20,
                2,
                Rarity.Legendary,
                ItemQuality.Exceptional,
                10),
            requirements[^1]);
        Assert.All(ratings.Zip(ratings.Skip(1)), pair =>
            Assert.True(pair.Second > pair.First,
                $"Late Tower rating regressed from {pair.First:F1} to {pair.Second:F1}."));
    }

    [Fact]
    public void Tier_two_epic_exceptional_build_materializes_ten_real_essences()
    {
        var services = CreateServices();
        var rung = services.Factory.GetProgressionLadder().Single(candidate =>
            candidate.Tier == 2
            && candidate.Rarity == Rarity.Epic
            && candidate.Quality == ItemQuality.Exceptional);

        var build = services.Factory.CreateBuild(
            CanonicalCooperativeRole.Guardian,
            rung,
            CanonicalEquipmentBuildFactory.MaximumCanonicalEssenceCount);

        Assert.Equal("t2-exceptional-epic", rung.Id);
        Assert.Equal(7, build.Equipment.Count);
        Assert.Equal(10, build.EquippedEssences.Count);
        Assert.All(build.Equipment, item =>
        {
            Assert.Equal(2, item.Tier);
            Assert.Equal(Rarity.Epic, item.Rarity);
            Assert.Equal(ItemQuality.Exceptional, item.Quality);
            Assert.NotEmpty(item.InstanceModifiers);
        });
        Assert.Equal(10, build.EquippedEssences
            .Select(essence => essence.EssenceDefinitionId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count());
    }

    [Fact]
    public void Fine_quality_is_available_to_canonical_calibration_builds()
    {
        var services = CreateServices();
        var rung = services.Factory.GetProgressionLadder().Single(candidate =>
            candidate.Tier == 2
            && candidate.Rarity == Rarity.Epic
            && candidate.Quality == ItemQuality.Fine);
        var build = services.Factory.CreateBuild(CanonicalCooperativeRole.Guardian, rung, 7);

        Assert.Equal("t2-fine-epic", rung.Id);
        Assert.All(build.Equipment, item => Assert.Equal(ItemQuality.Fine, item.Quality));
    }

    [Fact]
    public void Explicit_essence_build_preserves_the_requested_legal_combination()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);
        string[] requested = ["essence.raven", "essence.green_slime"];

        var first = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung, requested);
        var second = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung, requested);

        Assert.Equal(requested, first.EquippedEssences.Select(essence => essence.EssenceDefinitionId));
        Assert.Equal(
            first.EquippedEssences.Select(essence => essence.Id),
            second.EquippedEssences.Select(essence => essence.Id));
        Assert.Equal(first.Rating, second.Rating);
    }

    [Fact]
    public void Explicit_no_essence_build_is_a_complete_legal_control()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);

        var build = services.Factory.CreateBuild(
            CanonicalPartyProfile.Balanced,
            rung,
            Array.Empty<string>());

        Assert.Empty(build.EquippedEssences);
        Assert.Equal(7, build.Equipment.Count);
        Assert.True(build.Rating.Overall > 0);
    }

    [Fact]
    public async Task Persisted_snapshot_rehydration_matches_direct_canonical_world_tower_preparation()
    {
        var services = CreateServices();
        var rung = services.Factory.GetProgressionLadder().Single(candidate =>
            candidate.Tier == 2
            && candidate.Rarity == Rarity.Epic
            && candidate.Quality == ItemQuality.Exceptional);
        var build = services.Factory.CreateBuild(
            CanonicalCooperativeRole.Guardian,
            rung,
            CanonicalEquipmentBuildFactory.MaximumCanonicalEssenceCount);
        build.Character.ImagePath = "images/snapshots/guardian.webp";
        build.Character.EquipmentSlots = build.Equipment.Select(item => new EquipmentSlot
        {
            EntityId = build.Character.Id,
            Entity = build.Character,
            EquipmentInstanceId = item.Id,
            EquipmentInstance = item,
            EquipmentSlotType = ToSlot(item.EquipmentBase.EquipmentType)
        }).ToList();

        await using var db = CreateDbContext();
        db.ItemBases.AddRange(services.CraftingDefinitions.GetEquipmentBases().Values);
        var snapshotId = Guid.NewGuid();
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = build.Character.Id,
            Name = build.Character.Name,
            ImagePath = build.Character.ImagePath,
            Level = build.Character.Level,
            BaseAttributes = build.Character.BaseAttributes.Select(attribute =>
                new EntityAttributeSnapshot
                {
                    CharacterSnapshotId = snapshotId,
                    AttributeType = attribute.AttributeType,
                    Value = attribute.Value
                }).ToArray(),
            Equipment = build.Equipment.Select(item =>
                EquipmentSnapshot.From(ToSlot(item.EquipmentBase.EquipmentType), item)).ToArray(),
            EquippedEssences = build.EquippedEssences.Select((essence, index) =>
                EquippedEssenceSnapshot.From(snapshotId, index, essence)).ToArray()
        };
        db.CharacterSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.CharacterSnapshots.AsNoTracking()
            .Include(candidate => candidate.BaseAttributes)
            .Include(candidate => candidate.Equipment)
                .ThenInclude(item => item.InstanceModifiers)
            .Include(candidate => candidate.EquippedEssences)
            .SingleAsync(candidate => candidate.Id == snapshotId);
        var setup = new CombatSetupService(
            null!,
            services.EssenceResolver,
            services.EssenceDefinitions,
            services.CreatureEssences,
            craftingDefinitions: services.CraftingDefinitions);

        var direct = setup.CreatePlayerCombatEntities([build.Character]).Single();
        direct.EquippedEssences = [.. build.EquippedEssences];
        direct.HasEquippedEssenceSnapshot = true;
        await setup.PrepareEntitiesForCombat([direct], EssenceCombatActivity.WorldTower);

        var rehydratedParticipant = (await new SnapshotCombatantBuilder(db, setup).BuildAsync(
            [new SnapshotCombatantRequest(
                persisted,
                new CombatParticipantSlot(
                    build.Character.Id.ToString(),
                    build.Character.Id,
                    CombatSide.Friendly,
                    PartyNumber: 1))],
            CancellationToken.None)).Single();
        var rehydrated = rehydratedParticipant.Combatant;
        await setup.PrepareEntitiesForCombat([rehydrated], EssenceCombatActivity.WorldTower);

        Assert.Equal(build.Character.ImagePath, rehydratedParticipant.SourceEntity.ImagePath);
        Assert.Equal(direct.Level, rehydrated.Level);
        Assert.Equal(direct.CombatAttributes, rehydrated.CombatAttributes);
        Assert.Equivalent(EquipmentSignature(direct), EquipmentSignature(rehydrated), strict: true);
        Assert.Equivalent(EssenceSignature(direct), EssenceSignature(rehydrated), strict: true);
        Assert.Equal(direct.Tags.Order(StringComparer.OrdinalIgnoreCase),
            rehydrated.Tags.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(direct.NativeAbilityIds.Order(StringComparer.OrdinalIgnoreCase),
            rehydrated.NativeAbilityIds.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            direct.TemporaryAbilityModifiers.Select(modifier => modifier.ToString()),
            rehydrated.TemporaryAbilityModifiers.Select(modifier => modifier.ToString()));
    }

    [Fact]
    public void Tier_one_epic_balanced_profile_uses_crafted_gear_with_balanced_defenses()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);
        var build = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var projected = ProjectAttributes(build);

        Assert.InRange(projected[AttributeType.Armor], 35f, 45f);
        Assert.InRange(projected[AttributeType.Resistance], 35f, 45f);
        Assert.InRange(
            Math.Abs(projected[AttributeType.Armor] - projected[AttributeType.Resistance]),
            0f,
            5f);
        Assert.Equal(
            [
                "recipe.armor.chest.medium_mail",
                "recipe.armor.head.cloth_cowl",
                "recipe.armor.legs.light_legwraps"
            ],
            build.Equipment
                .Where(item => item.EquipmentBase.EquipmentType is
                    EquipmentType.Chest or EquipmentType.Head or EquipmentType.Legs)
                .Select(item => item.BaseRecipeId!)
                .Order()
                .ToArray());
        Assert.All(build.Equipment, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.BaseRecipeId));
            Assert.NotNull(services.CraftingDefinitions.GetRecipe(item.BaseRecipeId!));
            Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, item.StatModelVersion);
            Assert.NotEmpty(item.InstanceModifiers);
        });
    }

    [Fact]
    public void Balance_simulator_materializes_crafted_ratings_like_runtime_equipment()
    {
        var services = CreateServices();
        var rung = GetTierOneEpicRung(services.Factory);
        var build = services.Factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var expected = ProjectAttributes(build);
        var simulator = new AbilityBalanceSimulator(
            new JsonAbilityCatalogProvider(
                services.Configuration,
                services.ContentRoot,
                services.JsonOptions),
            services.EssenceDefinitions,
            services.Factory);

        var report = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 2,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: 8471,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams: null,
            EquipmentTier: 1,
            EquipmentRarity: "Epic",
            EquipmentProfile: "Balanced"));

        Assert.Equal(
            expected[AttributeType.Armor],
            report.ParticipantAttributes[AttributeType.Armor.ToString()],
            precision: 3);
        Assert.Equal(
            expected[AttributeType.Resistance],
            report.ParticipantAttributes[AttributeType.Resistance.ToString()],
            precision: 3);
    }

    private static IReadOnlyDictionary<AttributeType, float> ProjectAttributes(
        CanonicalEquipmentBuild build) =>
        CombatRatingCalculator.ProjectDirectAttributes(
            build.Character.BaseAttributes,
            AttributeCalculator.ProjectEquipmentModifiers(
                build.Equipment,
                build.Character.Level));

    private static object[] EquipmentSignature(Domain.Models.Combat.CombatEntity combatant) =>
        combatant.Equipment.OrderBy(item => item.ItemBaseId).Select(item => new
        {
            item.ItemBaseId,
            item.BaseRecipeId,
            item.BlueprintId,
            item.EquipmentSetId,
            item.Tier,
            item.Rarity,
            item.Quality,
            Modifiers = item.AttributeModifiers
                .OrderBy(modifier => modifier.AttributeType)
                .ThenBy(modifier => modifier.ModifierType)
                .Select(modifier => new
                {
                    modifier.AttributeType,
                    modifier.Amount,
                    modifier.ModifierType
                }).ToArray()
        }).Cast<object>().ToArray();

    private static object[] EssenceSignature(Domain.Models.Combat.CombatEntity combatant) =>
        combatant.EquippedEssences.OrderBy(essence => essence.EssenceDefinitionId).Select(essence => new
        {
            essence.EssenceDefinitionId,
            essence.Level,
            essence.CurrentXp,
            essence.AscensionTier,
            essence.IsEvolved
        }).Cast<object>().ToArray();

    private static EquipmentSlotType ToSlot(EquipmentType type) => type switch
    {
        EquipmentType.Head => EquipmentSlotType.Head,
        EquipmentType.Relic => EquipmentSlotType.Relic,
        EquipmentType.Chest => EquipmentSlotType.Chest,
        EquipmentType.Necklace => EquipmentSlotType.Necklace,
        EquipmentType.Legs => EquipmentSlotType.Legs,
        EquipmentType.Ring => EquipmentSlotType.Ring,
        EquipmentType.OneHanded or EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
        EquipmentType.OffHand => EquipmentSlotType.OffHand,
        EquipmentType.Tool => EquipmentSlotType.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static CanonicalEquipmentProgressionRung GetTierOneEpicRung(
        CanonicalEquipmentBuildFactory factory) =>
        factory.GetProgressionLadder().Single(candidate =>
            candidate.Tier == 1
            && candidate.Rarity == Rarity.Epic
            && candidate.Quality == ItemQuality.Standard);

    private static TestServices CreateServices()
    {
        var contentRoot = FindApiContentRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var balance = Options.Create(new CraftingBalanceOptions());
        var craftingDefinitions = new JsonCraftingDefinitionProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var factory = new CanonicalEquipmentBuildFactory(
            craftingDefinitions,
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        return new TestServices(
            configuration,
            contentRoot,
            jsonOptions,
            craftingDefinitions,
            essenceDefinitions,
            creatureEssences,
            essenceResolver,
            factory);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data")))
                return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL content root.");
    }

    private sealed record TestServices(
        IConfiguration Configuration,
        string ContentRoot,
        JsonSerializerOptions JsonOptions,
        JsonCraftingDefinitionProvider CraftingDefinitions,
        JsonEssenceDefinitionRepository EssenceDefinitions,
        JsonCreatureEssenceLootTableRepository CreatureEssences,
        EssenceSystemService EssenceResolver,
        CanonicalEquipmentBuildFactory Factory);
}
