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
                name: "DungeonRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DungeonDefinitionId = table.Column<string>(type: "text", nullable: false),
                    DungeonDefinitionName = table.Column<string>(type: "text", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentRoomIndex = table.Column<int>(type: "integer", nullable: false),
                    PendingExperience = table.Column<int>(type: "integer", nullable: false),
                    PendingCinders = table.Column<int>(type: "integer", nullable: false),
                    PendingSoulstones = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RewardsClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DungeonRuns", x => x.Id);
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
                    AttackSpeed = table.Column<int>(type: "integer", nullable: true),
                    Magnitude = table.Column<int>(type: "integer", nullable: true),
                    MagnitudeRange = table.Column<int>(type: "integer", nullable: true),
                    GatheringType = table.Column<int>(type: "integer", nullable: true),
                    YieldBonusPercent = table.Column<double>(type: "double precision", nullable: true),
                    RareChanceBonusPercent = table.Column<double>(type: "double precision", nullable: true),
                    DoubleGatherChancePercent = table.Column<double>(type: "double precision", nullable: true),
                    ScalingAttribute = table.Column<int>(type: "integer", nullable: true),
                    ScalingAmount = table.Column<float>(type: "real", nullable: true),
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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
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
                name: "RoomInstance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomIndex = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EncounterIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    EventOutcome = table.Column<int>(type: "integer", nullable: true),
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
                    Potential = table.Column<int>(type: "integer", nullable: true),
                    ItemXp = table.Column<int>(type: "integer", nullable: true),
                    IsMasterpiece = table.Column<bool>(type: "boolean", nullable: true),
                    IsLevelingItem = table.Column<bool>(type: "boolean", nullable: true)
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
                name: "LootTableEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: true),
                    LootTableType = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: true),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    IsRare = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootTableEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootTableEntry_ItemBases_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LootTableEntry_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CraftType = table.Column<int>(type: "integer", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_ItemBases_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "InstanceAttributeModifier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttributeType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<float>(type: "real", nullable: false),
                    ModifierType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceAttributeModifier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceAttributeModifier_EquipmentSnapshot_EquipmentSnapsh~",
                        column: x => x.EquipmentSnapshotId,
                        principalTable: "EquipmentSnapshot",
                        principalColumn: "Id");
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
                    SellerName = table.Column<string>(type: "text", nullable: false),
                    ItemInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Experience = table.Column<float>(type: "real", nullable: true),
                    Cinders = table.Column<long>(type: "bigint", nullable: true),
                    Soulstones = table.Column<long>(type: "bigint", nullable: true),
                    ArenaRating = table.Column<int>(type: "integer", nullable: true),
                    Archetype = table.Column<int>(type: "integer", nullable: true),
                    DamageProfile = table.Column<int>(type: "integer", nullable: true),
                    DefenseProfile = table.Column<int>(type: "integer", nullable: true),
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: true),
                    BaseLevel = table.Column<int>(type: "integer", nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: true),
                    ExperienceReward = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entities_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GatheringNodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: false),
                    ProfessionType = table.Column<int>(type: "integer", nullable: false),
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatheringNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatheringNodes_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ItemBaseId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => new { x.RecipeId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_Material_ItemBases_ItemBaseId",
                        column: x => x.ItemBaseId,
                        principalTable: "ItemBases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Material_ItemBases_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Material_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
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
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_AreaGatheringNode_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "ActionDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    CharacterTeam = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    AreaId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ProfessionType = table.Column<int>(type: "integer", nullable: true),
                    LootTableId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ActionDetails_LootTableEntry_LootTableId",
                        column: x => x.LootTableId,
                        principalTable: "LootTableEntry",
                        principalColumn: "Id",
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
                name: "GuildBuildingUpgrade",
                columns: table => new
                {
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingUpgradeDefinitionId = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBuildingUpgrade", x => new { x.GuildId, x.BuildingUpgradeDefinitionId });
                    table.ForeignKey(
                        name: "FK_GuildBuildingUpgrade_Guilds_GuildId",
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
                name: "IX_ActionDetails_LootTableId",
                table: "ActionDetails",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaCreature_CreatureId",
                table: "AreaCreature",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaGatheringNode_AreaId",
                table: "AreaGatheringNode",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaGatheringNode_LootTableId",
                table: "AreaGatheringNode",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_RegionId",
                table: "Areas",
                column: "RegionId");

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
                name: "IX_DungeonCompletionRecords_CharacterId_DungeonDefinitionId",
                table: "DungeonCompletionRecords",
                columns: new[] { "CharacterId", "DungeonDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entities_LootTableId",
                table: "Entities",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_UserId",
                table: "Entities",
                column: "UserId");

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
                name: "IX_GatheringNodes_LootTableId",
                table: "GatheringNodes",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildInvites_CharacterId",
                table: "GuildInvites",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMembers_CharacterId",
                table: "GuildMembers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_OwnerId",
                table: "Guilds",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanceAttributeModifier_EquipmentSnapshotId",
                table: "InstanceAttributeModifier",
                column: "EquipmentSnapshotId");

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
                name: "IX_LootTableEntry_ItemId",
                table: "LootTableEntry",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LootTableEntry_LootTableId",
                table: "LootTableEntry",
                column: "LootTableId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceListings_ItemInstanceId",
                table: "MarketPlaceListings",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_ItemBaseId",
                table: "Material",
                column: "ItemBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_ItemId",
                table: "Material",
                column: "ItemId");

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
                name: "IX_PlayerEssences_CharacterId",
                table: "PlayerEssences",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEssences_CharacterId_EssenceDefinitionId",
                table: "PlayerEssences",
                columns: new[] { "CharacterId", "EssenceDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ItemId",
                table: "Recipes",
                column: "ItemId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaCreature");

            migrationBuilder.DropTable(
                name: "AreaGatheringNode");

            migrationBuilder.DropTable(
                name: "ArenaTicketStatus");

            migrationBuilder.DropTable(
                name: "CharacterSoulstoneUpgrades");

            migrationBuilder.DropTable(
                name: "ColosseumMatches");

            migrationBuilder.DropTable(
                name: "CraftingQueueItems");

            migrationBuilder.DropTable(
                name: "DungeonCompletionRecords");

            migrationBuilder.DropTable(
                name: "EntityAttributes");

            migrationBuilder.DropTable(
                name: "EntityAttributeSnapshot");

            migrationBuilder.DropTable(
                name: "EquipmentSlots");

            migrationBuilder.DropTable(
                name: "EquippedEssenceSnapshots");

            migrationBuilder.DropTable(
                name: "EssenceLoadoutSlots");

            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "GatheringNodes");

            migrationBuilder.DropTable(
                name: "GuildBuildingUpgrade");

            migrationBuilder.DropTable(
                name: "GuildInvites");

            migrationBuilder.DropTable(
                name: "GuildMembers");

            migrationBuilder.DropTable(
                name: "GuildResource");

            migrationBuilder.DropTable(
                name: "InstanceAttributeModifier");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "ItemAttributeModifier");

            migrationBuilder.DropTable(
                name: "MarketPlaceListings");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "MonsterResonances");

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
                name: "ActionDetails");

            migrationBuilder.DropTable(
                name: "EssenceLoadouts");

            migrationBuilder.DropTable(
                name: "PlayerEssences");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "EquipmentSnapshot");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "ItemInstances");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "DungeonRuns");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "CharacterActions");

            migrationBuilder.DropTable(
                name: "CharacterSnapshots");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropTable(
                name: "LootTableEntry");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ItemBases");
        }
    }
}
