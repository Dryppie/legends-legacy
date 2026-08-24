using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Persistence.LL;
using Persistence.LL.Migrations;

namespace EssenceSystem.Tests;

public sealed class PersistentRangeConstraintTests
{
    [Fact]
    public void Model_enforces_non_negative_character_currency_balances()
    {
        using var db = CreateDbContext();
        var constraint = Assert.Single(
            db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Entity))!.GetCheckConstraints(),
            check => check.Name == "CK_Entities_CharacterCurrencyBalances_NonNegative");

        Assert.All(
            new[]
            {
                "Cinders",
                "Soulstones",
                "FateEcho",
                "SigilFragments",
                "GuildFavor",
                "TowerTokens",
                "RaidTrophies"
            },
            column => Assert.Contains($"\"{column}\" >= 0", constraint.Sql, StringComparison.Ordinal));
    }

    [Fact]
    public void Model_enforces_positive_inventory_quantities()
    {
        using var db = CreateDbContext();
        var constraint = Assert.Single(
            db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(InventoryItem))!.GetCheckConstraints(),
            check => check.Name == "CK_InventoryItems_Quantity_Positive");

        Assert.Equal("\"Quantity\" > 0", constraint.Sql);
    }

    [Fact]
    public void Model_enforces_arena_ticket_range()
    {
        using var db = CreateDbContext();
        var constraint = Assert.Single(
            db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ArenaTicketStatus))!.GetCheckConstraints(),
            check => check.Name == "CK_ArenaTicketStatus_CurrentTickets_Range");

        Assert.Equal("\"CurrentTickets\" >= 0 AND \"CurrentTickets\" <= 5", constraint.Sql);
    }

    [Fact]
    public void Migration_preflights_existing_rows_before_adding_constraints()
    {
        var operations = new EnforcePersistentValueRanges().UpOperations;
        var preflight = Assert.IsType<SqlOperation>(operations[0]);

        Assert.Contains("FROM \"Entities\"", preflight.Sql, StringComparison.Ordinal);
        Assert.Contains("FROM \"InventoryItems\"", preflight.Sql, StringComparison.Ordinal);
        Assert.Contains("FROM \"ArenaTicketStatus\"", preflight.Sql, StringComparison.Ordinal);
        Assert.Equal(
            [
                "CK_ArenaTicketStatus_CurrentTickets_Range",
                "CK_Entities_CharacterCurrencyBalances_NonNegative",
                "CK_InventoryItems_Quantity_Positive"
            ],
            operations
                .OfType<AddCheckConstraintOperation>()
                .Select(operation => operation.Name)
                .Order(StringComparer.Ordinal));
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
