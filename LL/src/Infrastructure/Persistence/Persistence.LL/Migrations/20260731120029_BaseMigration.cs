using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Persistence.LL.Migrations
{
    /// <inheritdoc />
    public partial class BaseMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Hint = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    PlayerSystemMessageTemplate = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    GlobalSystemMessageTemplate = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IconKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RequirementType = table.Column<int>(type: "integer", nullable: false),
                    RequirementTarget = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RequirementAmount = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementEventLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementEventLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChampionMarketPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    GloryCostPaid = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionMarketPurchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterCreatureArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatureDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatureName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KillCount = table.Column<int>(type: "integer", nullable: false),
                    IsEssenceFocus = table.Column<bool>(type: "boolean", nullable: false),
                    EssenceFocusSetAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceFocusTotalDurationSeconds = table.Column<long>(type: "bigint", nullable: false),
                    FirstDefeatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastDefeatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCreatureArchiveEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterDungeonMasteries",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DungeonDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CompletionCount = table.Column<int>(type: "integer", nullable: false),
                    LastAwardedRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDungeonMasteries", x => new { x.CharacterId, x.DungeonDefinitionId });
                });

            migrationBuilder.CreateTable(
                name: "CharacterRecipeMasteries",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRecipeMasteries", x => new { x.CharacterId, x.RecipeId });
                });

            migrationBuilder.CreateTable(
                name: "CharacterRecipeUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BlueprintId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRecipeUnlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTutorialProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorialId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CraftedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    EquippedTierOneEquipmentCount = table.Column<int>(type: "integer", nullable: false),
                    TrainingEssenceRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletionRewardGranted = table.Column<bool>(type: "boolean", nullable: false),
                    TrainingCombatWonAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceAbsorbedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EssenceEquippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTutorialProgresses", x => new { x.CharacterId, x.TutorialId });
                });

            migrationBuilder.CreateTable(
                name: "DailyProphecyRerollStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RerollsUsed = table.Column<int>(type: "integer", nullable: false),
                    FateEchoSpent = table.Column<long>(type: "bigint", nullable: false),
                    ShownDefinitionIdsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyProphecyRerollStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DungeonCompletionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DungeonDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FirstCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletionCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonCompletionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DungeonPowerRecommendationCacheEntries",
                columns: table => new
                {
                    DungeonId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DungeonTier = table.Column<int>(type: "integer", nullable: false),
                    DungeonContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AlgorithmVersion = table.Column<int>(type: "integer", nullable: false),
                    CombatRulesVersion = table.Column<int>(type: "integer", nullable: false),
                    BenchmarkDefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationSeedSetVersion = table.Column<int>(type: "integer", nullable: false),
                    EquipmentBalanceVersion = table.Column<int>(type: "integer", nullable: false),
                    RecommendationJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonPowerRecommendationCacheEntries", x => x.DungeonId);
                });

            migrationBuilder.CreateTable(
                name: "GameEventOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEventOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildContributionLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Metric = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    ContextId = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildContributionLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildMemberContributionPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    ContributionScore = table.Column<long>(type: "bigint", nullable: false),
                    GuildFavorEarned = table.Column<long>(type: "bigint", nullable: false),
                    GuildXpGenerated = table.Column<long>(type: "bigint", nullable: false),
                    GuildSuppliesGenerated = table.Column<long>(type: "bigint", nullable: false),
                    OrdersCompleted = table.Column<int>(type: "integer", nullable: false),
                    WeeklyMissionContribution = table.Column<long>(type: "bigint", nullable: false),
                    LastContributedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMemberContributionPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemBases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Stackable = table.Column<bool>(type: "boolean", nullable: false),
                    IsBound = table.Column<bool>(type: "boolean", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    EquipmentType = table.Column<int>(type: "integer", nullable: true),
                    GatheringType = table.Column<int>(type: "integer", nullable: true),
                    EssenceDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DismantleDustAmount = table.Column<int>(type: "integer", nullable: true, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemBases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonsterResonances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatureId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResonanceValue = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterResonances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEssences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EssenceDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CurrentXp = table.Column<int>(type: "integer", nullable: false),
                    NativeRegion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PotentialTier = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AscensionTier = table.Column<int>(type: "integer", nullable: false),
                    IsEvolved = table.Column<bool>(type: "boolean", nullable: false),
                    EvolutionUnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    AbsorbedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEssences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProphecyDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FlavorText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObjectiveText = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    ObjectiveType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ObjectiveParameterJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RewardProfileId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedSlots = table.Column<string>(type: "jsonb", nullable: false),
                    RequiredFeatures = table.Column<string>(type: "jsonb", nullable: false),
                    RequiredTags = table.Column<string>(type: "jsonb", nullable: false),
                    ExcludedTags = table.Column<string>(type: "jsonb", nullable: false),
                    MinPlayerLevel = table.Column<int>(type: "integer", nullable: false),
                    MaxPlayerLevel = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProphecyDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TitleDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsHiddenUntilUnlocked = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAchievementKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: true),
                    IconKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    MinParticipants = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    RegistrationDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartDelayAfterRegistrationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RoundIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinimumCharacterLevel = table.Column<int>(type: "integer", nullable: true),
                    MinimumArenaRating = table.Column<int>(type: "integer", nullable: true),
                    MinimumRankTier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    IsGuest = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsNameEdited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyRevelationProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PropheticFavor = table.Column<int>(type: "integer", nullable: false),
                    Milestone3Claimed = table.Column<bool>(type: "boolean", nullable: false),
                    Milestone5Claimed = table.Column<bool>(type: "boolean", nullable: false),
                    Milestone7Claimed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyRevelationProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAchievementProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AchievementDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: true),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    RequiredAmount = table.Column<long>(type: "bigint", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedByCharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievementProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAchievementProgresses_AchievementDefinitions_Achievem~",
                        column: x => x.AchievementDefinitionId,
                        principalTable: "AchievementDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArenaDefenseSnapshots",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadoutHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    IsOutdated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaDefenseSnapshots", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_ArenaDefenseSnapshots_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DungeonRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    DungeonDefinitionId = table.Column<string>(type: "text", nullable: false),
                    DungeonDefinitionName = table.Column<string>(type: "text", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentRoomIndex = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "jsonb", nullable: false),
                    PendingExperience = table.Column<int>(type: "integer", nullable: false),
                    PendingCinders = table.Column<int>(type: "integer", nullable: false),
                    PendingSoulstones = table.Column<int>(type: "integer", nullable: false),
                    DeathsDuringRun = table.Column<int>(type: "integer", nullable: false),
                    UsedRetreat = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardsClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DungeonRuns_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntityAttributeSnapshot",
                columns: table => new
                {
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAttributeSnapshot", x => new { x.CharacterSnapshotId, x.AttributeType });
                    table.ForeignKey(
                        name: "FK_EntityAttributeSnapshot_CharacterSnapshots_CharacterSnapsho~",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBaseId = table.Column<string>(type: "text", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    Potential = table.Column<int>(type: "integer", nullable: true),
                    ItemXp = table.Column<int>(type: "integer", nullable: false),
                    IsMasterpiece = table.Column<bool>(type: "boolean", nullable: false),
                    IsLevelingItem = table.Column<bool>(type: "boolean", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentSnapshot_CharacterSnapshots_CharacterSnapshotId",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquippedEssenceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    PlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EssenceDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NativeRegion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PotentialTier = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CurrentXp = table.Column<int>(type: "integer", nullable: false),
                    AscensionTier = table.Column<int>(type: "integer", nullable: false),
                    IsEvolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquippedEssenceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquippedEssenceSnapshots_CharacterSnapshots_CharacterSnapsh~",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameEventOutboxDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Consumer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEventOutboxDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameEventOutboxDeliveries_GameEventOutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "GameEventOutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemAttributeModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBaseId = table.Column<string>(type: "text", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAttributeModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAttributeModifier_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBaseId = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: true),
                    Quality = table.Column<int>(type: "integer", nullable: true),
                    BaseRecipeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BlueprintId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CraftedName = table.Column<string>(type: "text", nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: true),
                    Potential = table.Column<int>(type: "integer", nullable: true),
                    MaxPotential = table.Column<int>(type: "integer", nullable: true),
                    TemperingProgress = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: true),
                    ItemXp = table.Column<int>(type: "integer", nullable: true),
                    IsMasterpiece = table.Column<bool>(type: "boolean", nullable: true),
                    IsLevelingItem = table.Column<bool>(type: "boolean", nullable: true),
                    AffinityTags = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemInstances_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketPlaceBuyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceBuyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceBuyOrders_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketPlaceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    TotalPrice = table.Column<long>(type: "bigint", nullable: false),
                    SellerFee = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceOrders_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProphecyInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProphecyDefinitionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DailyRerollUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    CurrentValue = table.Column<int>(type: "integer", nullable: false),
                    ObjectiveParameterSnapshotJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    ProgressJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RewardSnapshotJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProphecyInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerProphecyInstances_ProphecyDefinitions_ProphecyDefinit~",
                        column: x => x.ProphecyDefinitionId,
                        principalTable: "ProphecyDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: false),
                    DifficultyTier = table.Column<int>(type: "integer", nullable: false),
                    SpawnProbabilities = table.Column<List<float>>(type: "real[]", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerTitleUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    TitleDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnlockedByAchievementDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<int>(type: "integer", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTitleUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTitleUnlocks_TitleDefinitions_TitleDefinitionId",
                        column: x => x.TitleDefinitionId,
                        principalTable: "TitleDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArenaTournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RegistrationStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationEndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MinParticipants = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    RoundIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    RegisteredParticipantCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaTournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArenaTournaments_TournamentDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "TournamentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Experience = table.Column<long>(type: "bigint", nullable: true),
                    Cinders = table.Column<long>(type: "bigint", nullable: true),
                    Soulstones = table.Column<long>(type: "bigint", nullable: true),
                    FateEcho = table.Column<long>(type: "bigint", nullable: true),
                    SigilFragments = table.Column<long>(type: "bigint", nullable: true),
                    GuildFavor = table.Column<long>(type: "bigint", nullable: true),
                    GuildHonors = table.Column<long>(type: "bigint", nullable: true),
                    EquippedTitleDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquippedTitleDisplayPosition = table.Column<int>(type: "integer", nullable: true),
                    Archetype = table.Column<int>(type: "integer", nullable: true),
                    DamageProfile = table.Column<int>(type: "integer", nullable: true),
                    DefenseProfile = table.Column<int>(type: "integer", nullable: true),
                    RewardTableId = table.Column<string>(type: "text", nullable: true),
                    BaseLevel = table.Column<int>(type: "integer", nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entities_TitleDefinitions_EquippedTitleDefinitionId",
                        column: x => x.EquippedTitleDefinitionId,
                        principalTable: "TitleDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderUserId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedBy = table.Column<string>(type: "text", nullable: true),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomInstance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomIndex = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EncounterIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInstance_DungeonRuns_DungeonRunId",
                        column: x => x.DungeonRunId,
                        principalTable: "DungeonRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RunRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    DungeonRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunRewards_DungeonRuns_DungeonRunId",
                        column: x => x.DungeonRunId,
                        principalTable: "DungeonRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAttributeModifierSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAttributeModifierSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAttributeModifierSnapshot_EquipmentSnapshot_Equipm~",
                        column: x => x.EquipmentSnapshotId,
                        principalTable: "EquipmentSnapshot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstanceAttributeModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceAttributeModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceAttributeModifier_ItemInstances_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketPlaceListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceListings_ItemInstances_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolBonusModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentBaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BonusType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolBonusModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolBonusModifier_ItemBases_EquipmentBaseId",
                        column: x => x.EquipmentBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToolBonusModifier_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaGatheringNode",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AreaId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: true),
                    ProcChance = table.Column<float>(type: "real", nullable: false),
                    RewardTableId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaGatheringNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaGatheringNode_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentCombatSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    PowerScore = table.Column<int>(type: "integer", nullable: true),
                    ArenaRatingAtSnapshot = table.Column<int>(type: "integer", nullable: false),
                    RankTierAtSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentCombatSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentCombatSnapshots_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentCombatSnapshots_CharacterSnapshots_CharacterSnaps~",
                        column: x => x.CharacterSnapshotId,
                        principalTable: "CharacterSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRewardGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Placement = table.Column<int>(type: "integer", nullable: true),
                    ArenaGlory = table.Column<int>(type: "integer", nullable: false),
                    Cinders = table.Column<int>(type: "integer", nullable: false),
                    Soulstones = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRewardGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRewardGrants_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRounds_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OwnerParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    EliminatedInRoundNumber = table.Column<int>(type: "integer", nullable: true),
                    FinalPlacement = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaCreature",
                columns: table => new
                {
                    AreaId = table.Column<string>(type: "text", nullable: false),
                    CreatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightedSpawnRate = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaCreature", x => new { x.AreaId, x.CreatureId });
                    table.ForeignKey(
                        name: "FK_AreaCreature_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaCreature_Entities_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArenaTicketStatus",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentTickets = table.Column<int>(type: "integer", nullable: false),
                    LastTicketUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaTicketStatus", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_ArenaTicketStatus_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterActions",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActions", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterActions_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterArenaProfiles",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    LifetimeHighestRating = table.Column<int>(type: "integer", nullable: false),
                    Glory = table.Column<int>(type: "integer", nullable: false),
                    CurrentAttackWinStreak = table.Column<int>(type: "integer", nullable: false),
                    BestAttackWinStreak = table.Column<int>(type: "integer", nullable: false),
                    AttackWins = table.Column<int>(type: "integer", nullable: false),
                    AttackDraws = table.Column<int>(type: "integer", nullable: false),
                    AttackLosses = table.Column<int>(type: "integer", nullable: false),
                    DefenseWins = table.Column<int>(type: "integer", nullable: false),
                    DefenseDraws = table.Column<int>(type: "integer", nullable: false),
                    DefenseLosses = table.Column<int>(type: "integer", nullable: false),
                    LastFirstWinBonusAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterArenaProfiles", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterArenaProfiles_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSoulstoneUpgrades",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoulstoneUpgradeDefinitionId = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSoulstoneUpgrades", x => new { x.CharacterId, x.SoulstoneUpgradeDefinitionId });
                    table.ForeignKey(
                        name: "FK_CharacterSoulstoneUpgrades_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColosseumMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterAId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterAName = table.Column<string>(type: "text", nullable: false),
                    CharacterARatingBefore = table.Column<int>(type: "integer", nullable: false),
                    CharacterARatingAfter = table.Column<int>(type: "integer", nullable: false),
                    CharacterBId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterBName = table.Column<string>(type: "text", nullable: false),
                    CharacterBRatingBefore = table.Column<int>(type: "integer", nullable: false),
                    CharacterBRatingAfter = table.Column<int>(type: "integer", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerName = table.Column<string>(type: "text", nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    CharacterARatingDelta = table.Column<int>(type: "integer", nullable: false),
                    CharacterBRatingDelta = table.Column<int>(type: "integer", nullable: false),
                    CharacterAGloryEarned = table.Column<int>(type: "integer", nullable: false),
                    CharacterBGloryEarned = table.Column<int>(type: "integer", nullable: false),
                    CharacterAStreakBefore = table.Column<int>(type: "integer", nullable: false),
                    CharacterAStreakAfter = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColosseumMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColosseumMatches_Entities_CharacterBId",
                        column: x => x.CharacterBId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityAttributes",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAttributes", x => new { x.EntityId, x.AttributeType });
                    table.ForeignKey(
                        name: "FK_EntityAttributes_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentSlots",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentSlotType = table.Column<int>(type: "integer", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentSlots", x => new { x.EntityId, x.EquipmentSlotType });
                    table.ForeignKey(
                        name: "FK_EquipmentSlots_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentSlots_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EssenceLoadouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssenceLoadouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EssenceLoadouts_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    GuildXp = table.Column<long>(type: "bigint", nullable: false),
                    GuildLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guilds_Entities_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_Inventories_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Professions",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessionType = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professions", x => new { x.CharacterId, x.ProfessionType });
                    table.ForeignKey(
                        name: "FK_Professions_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatOverride",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Multiplier = table.Column<float>(type: "real", nullable: true),
                    Additive = table.Column<float>(type: "real", nullable: true),
                    CreatureId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatOverride", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatOverride_Entities_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Entities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    MatchNumber = table.Column<int>(type: "integer", nullable: false),
                    PlayerOneParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlayerTwoParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoserParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    CombatSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BattleHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "TournamentRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsTeamOwner = table.Column<bool>(type: "boolean", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    EntryArenaRating = table.Column<int>(type: "integer", nullable: false),
                    EntryRankTier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EliminatedInRoundNumber = table.Column<int>(type: "integer", nullable: true),
                    FinalPlacement = table.Column<int>(type: "integer", nullable: true),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_TournamentCombatSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "TournamentCombatSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_TournamentTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeamApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeamApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeamApplications_TournamentTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeamInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeamInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeamInvites_TournamentTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    CharacterTeam = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    AreaId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionDetails_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionDetails_CharacterActions_CharacterActionId",
                        column: x => x.CharacterActionId,
                        principalTable: "CharacterActions",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EssenceLoadoutSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EssenceLoadoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    PlayerEssenceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssenceLoadoutSlots", x => x.Id);
                    table.CheckConstraint("CK_EssenceLoadoutSlots_SlotIndex", "\"SlotIndex\" >= 0 AND \"SlotIndex\" < 10");
                    table.ForeignKey(
                        name: "FK_EssenceLoadoutSlots_EssenceLoadouts_EssenceLoadoutId",
                        column: x => x.EssenceLoadoutId,
                        principalTable: "EssenceLoadouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EssenceLoadoutSlots_PlayerEssences_PlayerEssenceId",
                        column: x => x.PlayerEssenceId,
                        principalTable: "PlayerEssences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GuildActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildActivityLogs_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildBuildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TargetLevel = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBuildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildBuildings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildInvites",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInvite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildInvites", x => new { x.GuildId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_GuildInvites_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildInvites_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildMembers",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMembers", x => new { x.GuildId, x.CharacterId });
                    table.ForeignKey(
                        name: "FK_GuildMembers_Entities_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildMembers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<string>(type: "text", nullable: false),
                    TargetAmount = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionInstances_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekKey = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    SelectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SelectedByCharacterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionOptions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildResource",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Resource = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildResource", x => new { x.GuildId, x.Resource });
                    table.ForeignKey(
                        name: "FK_GuildResource_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildShopPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemKey = table.Column<string>(type: "text", nullable: false),
                    StockType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildShopPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildShopPurchases_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalGuildOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    TargetAmount = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAmount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalGuildOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalGuildOrders_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    InventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => new { x.InventoryId, x.ItemInstanceId });
                    table.ForeignKey(
                        name: "FK_InventoryItems_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_ItemInstances_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentCombatReplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerOneCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerTwoCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    CombatResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentCombatReplays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentCombatReplays_ArenaTournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "ArenaTournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentCombatReplays_TournamentMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CraftingQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EquipmentInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CraftType = table.Column<int>(type: "integer", nullable: false),
                    CraftingActionDetailsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraftingQueueItems_ActionDetails_CraftingActionDetailsId",
                        column: x => x.CraftingActionDetailsId,
                        principalTable: "ActionDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftingQueueItems_ItemInstances_EquipmentInstanceId",
                        column: x => x.EquipmentInstanceId,
                        principalTable: "ItemInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildMissionContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildMissionInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    ContributionTier = table.Column<int>(type: "integer", nullable: false),
                    LastContributedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMissionContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMissionContributions_GuildMissionInstances_GuildMissio~",
                        column: x => x.GuildMissionInstanceId,
                        principalTable: "GuildMissionInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementDefinitions_Key",
                table: "AchievementDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEventLedgers_CharacterId_ProcessedAt",
                table: "AchievementEventLedgers",
                columns: new[] { "CharacterId", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEventLedgers_OutboxMessageId",
                table: "AchievementEventLedgers",
                column: "OutboxMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionDetails_AreaId",
                table: "ActionDetails",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionDetails_CharacterActionId",
                table: "ActionDetails",
                column: "CharacterActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaCreature_CreatureId",
                table: "AreaCreature",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaGatheringNode_AreaId",
                table: "AreaGatheringNode",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_RegionId",
                table: "Areas",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaDefenseSnapshots_CharacterSnapshotId",
                table: "ArenaDefenseSnapshots",
                column: "CharacterSnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArenaTournaments_DefinitionId",
                table: "ArenaTournaments",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaTournaments_RegistrationStartsAtUtc_RegistrationEndsAt~",
                table: "ArenaTournaments",
                columns: new[] { "RegistrationStartsAtUtc", "RegistrationEndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ArenaTournaments_StartsAtUtc",
                table: "ArenaTournaments",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaTournaments_Status",
                table: "ArenaTournaments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ArenaTournaments_TournamentNumber",
                table: "ArenaTournaments",
                column: "TournamentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecutions_JobName_BusinessKey",
                table: "BackgroundJobExecutions",
                columns: new[] { "JobName", "BusinessKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChampionMarketPurchases_CharacterId_ItemId_PurchasedAt",
                table: "ChampionMarketPurchases",
                columns: new[] { "CharacterId", "ItemId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterArenaProfiles_Rating",
                table: "CharacterArenaProfiles",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCreatureArchiveEntries_CharacterId",
                table: "CharacterCreatureArchiveEntries",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCreatureArchiveEntries_CharacterId_CreatureDefinit~",
                table: "CharacterCreatureArchiveEntries",
                columns: new[] { "CharacterId", "CreatureDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCreatureArchiveEntries_CharacterId_IsEssenceFocus",
                table: "CharacterCreatureArchiveEntries",
                columns: new[] { "CharacterId", "IsEssenceFocus" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDungeonMasteries_DungeonDefinitionId",
                table: "CharacterDungeonMasteries",
                column: "DungeonDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId",
                table: "CharacterRecipeUnlocks",
                columns: new[] { "CharacterId", "RecipeId", "BlueprintId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTutorialProgresses_CurrentStep",
                table: "CharacterTutorialProgresses",
                column: "CurrentStep");

            migrationBuilder.CreateIndex(
                name: "IX_ColosseumMatches_CharacterBId",
                table: "ColosseumMatches",
                column: "CharacterBId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_CraftingActionDetailsId",
                table: "CraftingQueueItems",
                column: "CraftingActionDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingQueueItems_EquipmentInstanceId",
                table: "CraftingQueueItems",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyProphecyRerollStates_PlayerId_CharacterId_PeriodStart",
                table: "DailyProphecyRerollStates",
                columns: new[] { "PlayerId", "CharacterId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DungeonCompletionRecords_CharacterId_DungeonDefinitionId",
                table: "DungeonCompletionRecords",
                columns: new[] { "CharacterId", "DungeonDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DungeonRuns_CharacterId",
                table: "DungeonRuns",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DungeonRuns_CharacterSnapshotId",
                table: "DungeonRuns",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_EquippedTitleDefinitionId",
                table: "Entities",
                column: "EquippedTitleDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_NormalizedName",
                table: "Entities",
                column: "NormalizedName",
                unique: true,
                filter: "\"EntityType\" = 1 AND \"NormalizedName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_UserId",
                table: "Entities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAttributeModifierSnapshot_EquipmentSnapshotId",
                table: "EquipmentAttributeModifierSnapshot",
                column: "EquipmentSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentSlots_EquipmentInstanceId",
                table: "EquipmentSlots",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentSnapshot_CharacterSnapshotId",
                table: "EquipmentSnapshot",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_EquippedEssenceSnapshots_CharacterSnapshotId_SlotIndex",
                table: "EquippedEssenceSnapshots",
                columns: new[] { "CharacterSnapshotId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadouts_CharacterId",
                table: "EssenceLoadouts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadouts_CharacterId_Name",
                table: "EssenceLoadouts",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadoutSlots_EssenceLoadoutId_PlayerEssenceId",
                table: "EssenceLoadoutSlots",
                columns: new[] { "EssenceLoadoutId", "PlayerEssenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadoutSlots_EssenceLoadoutId_SlotIndex",
                table: "EssenceLoadoutSlots",
                columns: new[] { "EssenceLoadoutId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EssenceLoadoutSlots_PlayerEssenceId",
                table: "EssenceLoadoutSlots",
                column: "PlayerEssenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderUserId",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_Consumer_Status_AvailableAt",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "Consumer", "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_MessageId_Consumer",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "MessageId", "Consumer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxDeliveries_Status_AvailableAt_CreatedAt",
                table: "GameEventOutboxDeliveries",
                columns: new[] { "Status", "AvailableAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_AvailableAt_CreatedAt",
                table: "GameEventOutboxMessages",
                columns: new[] { "AvailableAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_CharacterId_CreatedAt",
                table: "GameEventOutboxMessages",
                columns: new[] { "CharacterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameEventOutboxMessages_IdempotencyKey",
                table: "GameEventOutboxMessages",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GuildActivityLogs_GuildId_CreatedAt",
                table: "GuildActivityLogs",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildBuildings_GuildId_Type",
                table: "GuildBuildings",
                columns: new[] { "GuildId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildContributionLedgers_GuildId_CharacterId_OccurredAt",
                table: "GuildContributionLedgers",
                columns: new[] { "GuildId", "CharacterId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildContributionLedgers_IdempotencyKey",
                table: "GuildContributionLedgers",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildInvites_CharacterId",
                table: "GuildInvites",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberContributionPeriods_GuildId_CharacterId_PeriodTy~",
                table: "GuildMemberContributionPeriods",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberContributionPeriods_LastContributedAt",
                table: "GuildMemberContributionPeriods",
                column: "LastContributedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMembers_CharacterId",
                table: "GuildMembers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionContributions_GuildMissionInstanceId_CharacterId",
                table: "GuildMissionContributions",
                columns: new[] { "GuildMissionInstanceId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionInstances_GuildId_WeekKey",
                table: "GuildMissionInstances",
                columns: new[] { "GuildId", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildMissionOptions_GuildId_WeekKey_IsSelected",
                table: "GuildMissionOptions",
                columns: new[] { "GuildId", "WeekKey", "IsSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_OwnerId",
                table: "Guilds",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildShopPurchases_GuildId_CharacterId_ShopItemKey_PeriodKey",
                table: "GuildShopPurchases",
                columns: new[] { "GuildId", "CharacterId", "ShopItemKey", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanceAttributeModifier_ItemInstanceId",
                table: "InstanceAttributeModifier",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ItemInstanceId",
                table: "InventoryItems",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributeModifier_ItemBaseId",
                table: "ItemAttributeModifier",
                column: "ItemBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBases_EssenceDefinitionId",
                table: "ItemBases",
                column: "EssenceDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstances_ItemBaseId",
                table: "ItemInstances",
                column: "ItemBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_BuyerId",
                table: "MarketPlaceBuyOrders",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_ExpiresAt",
                table: "MarketPlaceBuyOrders",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceBuyOrders_ItemBaseId_UnitPrice_CreatedAt",
                table: "MarketPlaceBuyOrders",
                columns: new[] { "ItemBaseId", "UnitPrice", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_ExpiresAt",
                table: "MarketPlaceListings",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_ItemInstanceId",
                table: "MarketPlaceListings",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_SellerId",
                table: "MarketPlaceListings",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_UnitPrice_CreatedAt",
                table: "MarketPlaceListings",
                columns: new[] { "UnitPrice", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_BuyerId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "BuyerId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_ItemBaseId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "ItemBaseId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceOrders_SellerId_PurchasedAt",
                table: "MarketPlaceOrders",
                columns: new[] { "SellerId", "PurchasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonsterResonances_CharacterId",
                table: "MonsterResonances",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterResonances_CharacterId_CreatureId",
                table: "MonsterResonances",
                columns: new[] { "CharacterId", "CreatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGuildOrders_GuildId_CharacterId_PeriodType_PeriodKey",
                table: "PersonalGuildOrders",
                columns: new[] { "GuildId", "CharacterId", "PeriodType", "PeriodKey" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievementProgresses_AccountId_CharacterId_Achieveme~",
                table: "PlayerAchievementProgresses",
                columns: new[] { "AccountId", "CharacterId", "AchievementDefinitionId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievementProgresses_AchievementDefinitionId",
                table: "PlayerAchievementProgresses",
                column: "AchievementDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEssences_CharacterId",
                table: "PlayerEssences",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEssences_CharacterId_EssenceDefinitionId",
                table: "PlayerEssences",
                columns: new[] { "CharacterId", "EssenceDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProphecyInstances_PlayerId_CharacterId_Scope_PeriodS~1",
                table: "PlayerProphecyInstances",
                columns: new[] { "PlayerId", "CharacterId", "Scope", "PeriodStart", "SlotType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProphecyInstances_PlayerId_CharacterId_Scope_PeriodSt~",
                table: "PlayerProphecyInstances",
                columns: new[] { "PlayerId", "CharacterId", "Scope", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProphecyInstances_PlayerId_CharacterId_Status",
                table: "PlayerProphecyInstances",
                columns: new[] { "PlayerId", "CharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProphecyInstances_ProphecyDefinitionId",
                table: "PlayerProphecyInstances",
                column: "ProphecyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTitleUnlocks_AccountId_CharacterId_TitleDefinitionId_~",
                table: "PlayerTitleUnlocks",
                columns: new[] { "AccountId", "CharacterId", "TitleDefinitionId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTitleUnlocks_TitleDefinitionId",
                table: "PlayerTitleUnlocks",
                column: "TitleDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProphecyDefinitions_Scope_Category_Difficulty_IsEnabled",
                table: "ProphecyDefinitions",
                columns: new[] { "Scope", "Category", "Difficulty", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AppUserId",
                table: "RefreshTokens",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInstance_DungeonRunId",
                table: "RoomInstance",
                column: "DungeonRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RunRewards_DungeonRunId",
                table: "RunRewards",
                column: "DungeonRunId");

            migrationBuilder.CreateIndex(
                name: "IX_StatOverride_CreatureId",
                table: "StatOverride",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleDefinitions_Key",
                table: "TitleDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolBonusModifier_EquipmentBaseId",
                table: "ToolBonusModifier",
                column: "EquipmentBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolBonusModifier_EquipmentInstanceId",
                table: "ToolBonusModifier",
                column: "EquipmentInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatReplays_BattleHistoryId",
                table: "TournamentCombatReplays",
                column: "BattleHistoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatReplays_CombatSessionId",
                table: "TournamentCombatReplays",
                column: "CombatSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatReplays_MatchId",
                table: "TournamentCombatReplays",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatReplays_TournamentId_MatchId",
                table: "TournamentCombatReplays",
                columns: new[] { "TournamentId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatSnapshots_CharacterId",
                table: "TournamentCombatSnapshots",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatSnapshots_CharacterSnapshotId",
                table: "TournamentCombatSnapshots",
                column: "CharacterSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentCombatSnapshots_TournamentId_CharacterId",
                table: "TournamentCombatSnapshots",
                columns: new[] { "TournamentId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentDefinitions_Enabled",
                table: "TournamentDefinitions",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentDefinitions_Key",
                table: "TournamentDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_RoundId",
                table: "TournamentMatches",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_RoundNumber_MatchNumber",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "RoundNumber", "MatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_Status",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_WinnerParticipantId",
                table: "TournamentMatches",
                column: "WinnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_CharacterId",
                table: "TournamentParticipants",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_SnapshotId",
                table: "TournamentParticipants",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_Status",
                table: "TournamentParticipants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TeamId",
                table: "TournamentParticipants",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_AccountId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_CharacterId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_Seed",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "Seed" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_TeamId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRewardGrants_CharacterId_Status",
                table: "TournamentRewardGrants",
                columns: new[] { "CharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRewardGrants_TournamentId_CharacterId_RewardKey",
                table: "TournamentRewardGrants",
                columns: new[] { "TournamentId", "CharacterId", "RewardKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_StartsAtUtc",
                table: "TournamentRounds",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_TournamentId_RoundNumber",
                table: "TournamentRounds",
                columns: new[] { "TournamentId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_TournamentId_Status",
                table: "TournamentRounds",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamApplications_TeamId_ApplicantParticipantId_St~",
                table: "TournamentTeamApplications",
                columns: new[] { "TeamId", "ApplicantParticipantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamApplications_TournamentId_ApplicantParticipan~",
                table: "TournamentTeamApplications",
                columns: new[] { "TournamentId", "ApplicantParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamInvites_TeamId_InvitedParticipantId_Status",
                table: "TournamentTeamInvites",
                columns: new[] { "TeamId", "InvitedParticipantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeamInvites_TournamentId_InvitedParticipantId",
                table: "TournamentTeamInvites",
                columns: new[] { "TournamentId", "InvitedParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_OwnerParticipantId",
                table: "TournamentTeams",
                column: "OwnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Name",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Seed",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Seed" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_Status",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyRevelationProgress_PlayerId_CharacterId_PeriodStart",
                table: "WeeklyRevelationProgress",
                columns: new[] { "PlayerId", "CharacterId", "PeriodStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementEventLedgers");

            migrationBuilder.DropTable(
                name: "AreaCreature");

            migrationBuilder.DropTable(
                name: "AreaGatheringNode");

            migrationBuilder.DropTable(
                name: "ArenaDefenseSnapshots");

            migrationBuilder.DropTable(
                name: "ArenaTicketStatus");

            migrationBuilder.DropTable(
                name: "BackgroundJobExecutions");

            migrationBuilder.DropTable(
                name: "ChampionMarketPurchases");

            migrationBuilder.DropTable(
                name: "CharacterArenaProfiles");

            migrationBuilder.DropTable(
                name: "CharacterCreatureArchiveEntries");

            migrationBuilder.DropTable(
                name: "CharacterDungeonMasteries");

            migrationBuilder.DropTable(
                name: "CharacterRecipeMasteries");

            migrationBuilder.DropTable(
                name: "CharacterRecipeUnlocks");

            migrationBuilder.DropTable(
                name: "CharacterSoulstoneUpgrades");

            migrationBuilder.DropTable(
                name: "CharacterTutorialProgresses");

            migrationBuilder.DropTable(
                name: "ColosseumMatches");

            migrationBuilder.DropTable(
                name: "CraftingQueueItems");

            migrationBuilder.DropTable(
                name: "DailyProphecyRerollStates");

            migrationBuilder.DropTable(
                name: "DungeonCompletionRecords");

            migrationBuilder.DropTable(
                name: "DungeonPowerRecommendationCacheEntries");

            migrationBuilder.DropTable(
                name: "EntityAttributes");

            migrationBuilder.DropTable(
                name: "EntityAttributeSnapshot");

            migrationBuilder.DropTable(
                name: "EquipmentAttributeModifierSnapshot");

            migrationBuilder.DropTable(
                name: "EquipmentSlots");

            migrationBuilder.DropTable(
                name: "EquippedEssenceSnapshots");

            migrationBuilder.DropTable(
                name: "EssenceLoadoutSlots");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "GameEventOutboxDeliveries");

            migrationBuilder.DropTable(
                name: "GuildActivityLogs");

            migrationBuilder.DropTable(
                name: "GuildBuildings");

            migrationBuilder.DropTable(
                name: "GuildContributionLedgers");

            migrationBuilder.DropTable(
                name: "GuildInvites");

            migrationBuilder.DropTable(
                name: "GuildMemberContributionPeriods");

            migrationBuilder.DropTable(
                name: "GuildMembers");

            migrationBuilder.DropTable(
                name: "GuildMissionContributions");

            migrationBuilder.DropTable(
                name: "GuildMissionOptions");

            migrationBuilder.DropTable(
                name: "GuildResource");

            migrationBuilder.DropTable(
                name: "GuildShopPurchases");

            migrationBuilder.DropTable(
                name: "InstanceAttributeModifier");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "ItemAttributeModifier");

            migrationBuilder.DropTable(
                name: "MarketPlaceBuyOrders");

            migrationBuilder.DropTable(
                name: "MarketPlaceListings");

            migrationBuilder.DropTable(
                name: "MarketPlaceOrders");

            migrationBuilder.DropTable(
                name: "MonsterResonances");

            migrationBuilder.DropTable(
                name: "PersonalGuildOrders");

            migrationBuilder.DropTable(
                name: "PlayerAchievementProgresses");

            migrationBuilder.DropTable(
                name: "PlayerProphecyInstances");

            migrationBuilder.DropTable(
                name: "PlayerTitleUnlocks");

            migrationBuilder.DropTable(
                name: "Professions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoomInstance");

            migrationBuilder.DropTable(
                name: "RunRewards");

            migrationBuilder.DropTable(
                name: "StatOverride");

            migrationBuilder.DropTable(
                name: "ToolBonusModifier");

            migrationBuilder.DropTable(
                name: "TournamentCombatReplays");

            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "TournamentRewardGrants");

            migrationBuilder.DropTable(
                name: "TournamentTeamApplications");

            migrationBuilder.DropTable(
                name: "TournamentTeamInvites");

            migrationBuilder.DropTable(
                name: "WeeklyRevelationProgress");

            migrationBuilder.DropTable(
                name: "ActionDetails");

            migrationBuilder.DropTable(
                name: "EquipmentSnapshot");

            migrationBuilder.DropTable(
                name: "EssenceLoadouts");

            migrationBuilder.DropTable(
                name: "PlayerEssences");

            migrationBuilder.DropTable(
                name: "GameEventOutboxMessages");

            migrationBuilder.DropTable(
                name: "GuildMissionInstances");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "AchievementDefinitions");

            migrationBuilder.DropTable(
                name: "ProphecyDefinitions");

            migrationBuilder.DropTable(
                name: "DungeonRuns");

            migrationBuilder.DropTable(
                name: "ItemInstances");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "TournamentCombatSnapshots");

            migrationBuilder.DropTable(
                name: "TournamentTeams");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "CharacterActions");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "ItemBases");

            migrationBuilder.DropTable(
                name: "TournamentRounds");

            migrationBuilder.DropTable(
                name: "CharacterSnapshots");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropTable(
                name: "ArenaTournaments");

            migrationBuilder.DropTable(
                name: "TitleDefinitions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "TournamentDefinitions");
        }
    }
}
