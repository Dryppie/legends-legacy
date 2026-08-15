using Domain.Models.Entities.Characters;
using Domain.Models.Economy;
using Domain.Models.Transfers;
using Application.Interfaces.Services.LL.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Entities.Characters;
using Services.LL.Entities.Characters;

namespace EssenceSystem.Tests;

public sealed class CurrencyTransferTests
{
    [Fact]
    public async Task Wire_moves_cinders_and_resolves_recipient_name_case_insensitively()
    {
        await using var db = CreateDb();
        var sender = AddCharacter(db, "Sender", 1_000);
        var recipient = AddCharacter(db, "Ember", 25);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.TransferCindersAsync(
            sender.Id,
            "eMbEr",
            250,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(750, sender.Cinders);
        Assert.Equal(275, recipient.Cinders);
        Assert.Equal(987_654, result.Sender!.ExperienceUntilNextLevel);
        Assert.Equal(987_654, result.Recipient!.ExperienceUntilNextLevel);
        var history = await db.PlayerTransferHistory.SingleAsync();
        Assert.Equal(PlayerTransferKind.Cinders, history.Kind);
        Assert.Equal(sender.UserId, history.SenderAccountId);
        Assert.Equal(recipient.UserId, history.RecipientAccountId);
        Assert.Equal(250, history.Quantity);
        var ledgerEntry = await db.EconomyLedger.SingleAsync();
        Assert.Equal(EconomyEventType.DirectCurrencyTransfer, ledgerEntry.EventType);
        Assert.Equal(sender.UserId, ledgerEntry.SenderAccountId);
        Assert.Equal(recipient.UserId, ledgerEntry.RecipientAccountId);
        Assert.Equal(250, ledgerEntry.TotalValue);
    }

    [Fact]
    public async Task Wire_rejects_an_overdraft_without_changing_balances()
    {
        await using var db = CreateDb();
        var sender = AddCharacter(db, "Sender", 100);
        var recipient = AddCharacter(db, "Recipient", 50);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.TransferCindersAsync(
            sender.Id,
            recipient.Name,
            101,
            CancellationToken.None);

        Assert.Equal(CinderTransferFailure.InsufficientCinders, result.Failure);
        Assert.Equal(100, sender.Cinders);
        Assert.Equal(50, recipient.Cinders);
        Assert.Empty(db.PlayerTransferHistory);
    }

    [Fact]
    public async Task Wire_rejects_self_transfer()
    {
        await using var db = CreateDb();
        var sender = AddCharacter(db, "Sender", 100);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.TransferCindersAsync(
            sender.Id,
            sender.Name,
            10,
            CancellationToken.None);

        Assert.Equal(CinderTransferFailure.SameRecipient, result.Failure);
        Assert.Equal(100, sender.Cinders);
        Assert.Empty(db.PlayerTransferHistory);
    }

    [Fact]
    public async Task Wire_rejects_recipient_balance_overflow()
    {
        await using var db = CreateDb();
        var sender = AddCharacter(db, "Sender", 100);
        var recipient = AddCharacter(db, "Recipient", long.MaxValue - 5);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.TransferCindersAsync(
            sender.Id,
            recipient.Name,
            10,
            CancellationToken.None);

        Assert.Equal(CinderTransferFailure.RecipientBalanceOverflow, result.Failure);
        Assert.Equal(100, sender.Cinders);
        Assert.Equal(long.MaxValue - 5, recipient.Cinders);
        Assert.Empty(db.PlayerTransferHistory);
    }

    private static CurrencyTransferService CreateService(LLDbContext db) =>
        new(
            new CharacterRepository(db),
            new CurrencyTransferRepository(db),
            new TestExperienceProgressionProvider());

    private sealed class TestExperienceProgressionProvider : ICharacterExperienceProgressionProvider
    {
        public long GetRequiredExperience(int level) => 987_654;
    }

    private static Character AddCharacter(LLDbContext db, string name, long cinders)
    {
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            Cinders = cinders
        };
        character.NormalizeName();
        db.Characters.Add(character);
        return character;
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
