using Domain.Models.CombatStyles;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Persistence.LL;
using Persistence.LL.Migrations;

namespace EssenceSystem.Tests;

public sealed class CombatStyleMigrationTests
{
    [Fact]
    public void Model_has_one_global_selection_and_preserves_progression_and_snapshots()
    {
        using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var model = db.Model;
        var selection = model.FindEntityType(typeof(CharacterCombatStyleSelection));
        Assert.NotNull(selection);
        Assert.Equal([nameof(CharacterCombatStyleSelection.CharacterId)],
            selection.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.Null(selection.FindProperty("Activity"));
        Assert.True(selection.FindProperty(nameof(CharacterCombatStyleSelection.MasteredUpgradeId))!.IsNullable);
        Assert.Equal(64, selection.FindProperty(nameof(CharacterCombatStyleSelection.MasteredUpgradeId))!.GetMaxLength());

        var character = model.FindEntityType(typeof(Character));
        Assert.NotNull(character);
        Assert.Null(character.FindProperty("BastionPracticeCompleted"));
        Assert.Null(character.FindProperty("ConduitPracticeCompleted"));
        Assert.Null(character.FindProperty("CombatStylesIntroductionCompletedAt"));
        Assert.DoesNotContain(model.GetEntityTypes(), x => x.Name.StartsWith("Domain.Models.CombatStyles.CharacterBuild", StringComparison.Ordinal));
        Assert.Null(model.FindEntityType(typeof(EssenceLoadout))!.FindProperty("IsDefault"));

        var progression = model.FindEntityType(typeof(CharacterCombatStyle));
        Assert.NotNull(progression);
        Assert.NotNull(progression.FindProperty(nameof(CharacterCombatStyle.Level)));
        Assert.NotNull(progression.FindProperty(nameof(CharacterCombatStyle.CurrentXp)));
        Assert.True(progression.FindProperty(nameof(CharacterCombatStyle.MasteredUpgradeId))!.IsNullable);
        Assert.Equal(64, progression.FindProperty(nameof(CharacterCombatStyle.MasteredUpgradeId))!.GetMaxLength());
        Assert.NotNull(model.FindEntityType(typeof(CharacterSnapshot))!.FindProperty(nameof(CharacterSnapshot.CombatStyle)));
    }

    [Fact]
    public void Mastery_migration_only_adds_optional_selection_and_remembered_choice_columns()
    {
        var migration = new AddCombatStyleUpgradeMastery();
        Assert.Equal(2, migration.UpOperations.Count);
        var columns = migration.UpOperations.Cast<AddColumnOperation>().OrderBy(x => x.Table, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "CharacterCombatStyleSelections", "CharacterCombatStyles" }, columns.Select(x => x.Table));
        Assert.All(columns, column =>
        {
            Assert.Equal("MasteredUpgradeId", column.Name);
            Assert.True(column.IsNullable);
            Assert.Equal(64, column.MaxLength);
            Assert.Null(column.DefaultValue);
        });
        Assert.Equal(2, migration.DownOperations.Count);
        Assert.All(migration.DownOperations.Cast<DropColumnOperation>(), column => Assert.Equal("MasteredUpgradeId", column.Name));
    }

    [Fact]
    public void Forward_migration_keeps_only_former_default_before_collapsing_the_selection_key()
    {
        var operations = new MakeCombatStylesGlobal().UpOperations.ToList();
        var retireOverrides = Assert.Single(operations.OfType<SqlOperation>());
        Assert.Equal("DELETE FROM \"CharacterCombatStyleSelections\" WHERE \"Activity\" <> 0;", retireOverrides.Sql.Trim());
        var dropActivity = Assert.Single(operations.OfType<DropColumnOperation>(),
            x => x.Table == "CharacterCombatStyleSelections" && x.Name == "Activity");
        var dropKey = Assert.Single(operations.OfType<DropPrimaryKeyOperation>());
        Assert.True(operations.IndexOf(retireOverrides) < operations.IndexOf(dropActivity));
        Assert.True(operations.IndexOf(retireOverrides) < operations.IndexOf(dropKey));
        var newKey = Assert.Single(operations.OfType<AddPrimaryKeyOperation>());
        Assert.Equal("CharacterCombatStyleSelections", newKey.Table);
        Assert.Equal(["CharacterId"], newKey.Columns);

        Assert.Equal(new[] { "CharacterBuildEquipmentSelections", "CharacterBuildEssenceSelections", "CharacterBuildPresets" },
            operations.OfType<DropTableOperation>().Select(x => x.Name).Order());
        Assert.DoesNotContain(operations.OfType<DropColumnOperation>(),
            x => x.Table is "CharacterCombatStyles" or "CharacterSnapshots");
    }

    [Fact]
    public void Downgrade_restores_global_choice_as_default_and_materializable_character_flags()
    {
        var operations = new MakeCombatStylesGlobal().DownOperations;
        var activity = Assert.Single(operations.OfType<AddColumnOperation>(),
            x => x.Table == "CharacterCombatStyleSelections" && x.Name == "Activity");
        Assert.Equal(0, activity.DefaultValue);
        Assert.False(activity.IsNullable);
        var key = Assert.Single(operations.OfType<AddPrimaryKeyOperation>());
        Assert.Equal(new[] { "CharacterId", "Activity" }, key.Columns);
        var backfill = Assert.Single(operations.OfType<SqlOperation>());
        Assert.Contains("\"BastionPracticeCompleted\" = FALSE", backfill.Sql);
        Assert.Contains("\"ConduitPracticeCompleted\" = FALSE", backfill.Sql);
        Assert.Contains("WHERE \"EntityType\" = 1", backfill.Sql);
    }
}
