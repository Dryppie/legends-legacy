using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Behaviors;
using Application.UseCases.Equipments.Commands.ImproveEquipmentProgressionRank;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;
using Services.LL.Outbox;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed partial class ForgeTests
{
    private const string Fury = "blueprint_fury";
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Preview_is_read_only_and_maps_exact_item_and_price_results()
    {
        await using var f = await Fixture.Create();
        var quote = await f.Preview(ForgeOperationKind.ImproveRank);
        Assert.True(quote.CanExecute, quote.UnavailableReason);
        Assert.Equal(5, quote.ScrapCost);
        Assert.Equal(250, quote.CinderCost);
        Assert.Equal(0, quote.Before!.State.Rank);
        Assert.Equal(1, quote.After!.State.Rank);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        var dto = Mapper().Map<ForgeQuoteDto>(quote);
        Assert.Equal(quote.After.Stats, dto.After!.Stats);
        Assert.Equal(quote.Token, dto.Token);
        Assert.Equal(quote.OperationId, dto.OperationId);
        Assert.Empty(await f.Db.ForgeReceipts.ToListAsync());
        Assert.Empty(await f.Db.GameEventOutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Rank_spends_exact_cost_across_stacks_binds_and_replays_without_double_charging()
    {
        await using var f = await Fixture.Create(scrap: 3);
        await f.AddStack("tempered_scrap", 2);
        var originalSource = f.Gear.ProgressionData!.State.Provenance;
        var quote = await f.Preview(ForgeOperationKind.ImproveRank);
        var result = await f.Execute(quote);
        Assert.Null(result.Error);
        Assert.Equal(1, f.Gear.ProgressionData!.State.Rank);
        Assert.Equal(5, f.Gear.ProgressionData.EquipmentState.PaidScrap);
        Assert.Equal(originalSource, f.Gear.ProgressionData.State.Provenance);
        Assert.True(f.Gear.IsBound);
        Assert.Equal(9750, f.Character.Cinders);
        Assert.Equal(0, await f.Scrap());
        f.Db.ChangeTracker.Clear();
        f.Clock.Now = f.Clock.Now.AddHours(1);
        var replay = await f.Execute(quote);
        Assert.Equal(result.Outcome!.After!.Serialize(), replay.Outcome!.After!.Serialize());
        Assert.Single(await f.Db.ForgeReceipts.ToListAsync());
        Assert.Equal(9750, (await f.Db.Characters.SingleAsync()).Cinders);
        var ledger = await f.Db.EconomyLedger.ToListAsync();
        Assert.Equal(5, ledger.Where(x => x.AssetId == "tempered_scrap").Sum(x => x.Quantity));
        Assert.Equal(250, Assert.Single(ledger, x => x.AssetId == "currency:cinders").Quantity);
        Assert.All(ledger, x => Assert.Equal(quote.OperationId, x.ReferenceId));
        Assert.Equal(GameEventTypes.ForgeCompleted, Assert.Single(await f.Db.GameEventOutboxMessages.ToListAsync()).EventType);
        Assert.Null((await f.Service.ExecuteAsync(f.Character.Id, quote.OperationId,
            quote.Request with { Kind = ForgeOperationKind.Salvage }, quote.Token, Ct)).Outcome);
    }

    [Theory]
    [InlineData(4, 10000, "Scrap")]
    [InlineData(10, 249, "Cinders")]
    public async Task Insufficient_currency_has_no_partial_debit_or_receipt(int scrap, long cinders, string message)
    {
        await using var f = await Fixture.Create(scrap, cinders);
        var quote = await f.Preview(ForgeOperationKind.ImproveRank);
        Assert.False(quote.CanExecute);
        var result = await f.Execute(quote);
        Assert.Contains(message, result.Error);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.Equal(cinders, f.Character.Cinders);
        Assert.Equal(scrap, await f.Scrap());
        Assert.Empty(await f.Db.ForgeReceipts.ToListAsync());
    }

    [Fact]
    public async Task Rank_cap_and_stale_item_price_and_expired_quotes_never_charge()
    {
        await using var f = await Fixture.Create(scrap: 1000);
        var old = await f.Preview(ForgeOperationKind.ImproveRank);
        f.Clock.Now = old.ExpiresAtUtc;
        var expired = await f.Execute(old);
        Assert.NotNull(expired.FreshQuote);
        Assert.Equal(10000, f.Character.Cinders);
        var current = expired.FreshQuote!;
        var changedPrices = new ForgePrices(3, [new(1, [6, 10, 20, 40, 80], [250, 500, 1000, 2000, 4000], 250)], .5m);
        var changed = await f.ServiceWith(changedPrices).ExecuteAsync(f.Character.Id, current.OperationId, current.Request, current.Token, Ct);
        Assert.Equal(6, changed.FreshQuote!.ScrapCost);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        await f.Execute(current);
        var stale = await f.Service.ExecuteAsync(f.Character.Id, Guid.NewGuid(), current.Request, current.Token, Ct);
        Assert.Null(stale.Outcome);
        Assert.Equal(1, stale.FreshQuote!.Before!.State.Rank);
        for (var rank = 2; rank <= 5; rank++)
        {
            var next = await f.Execute(await f.Preview(ForgeOperationKind.ImproveRank));
            Assert.Equal(rank, next.Outcome!.After!.State.Rank);
        }
        var max = await f.Preview(ForgeOperationKind.ImproveRank);
        Assert.False(max.CanExecute);
        Assert.Contains("rank 5", max.UnavailableReason);
        Assert.Equal(155, f.Gear.ProgressionData!.EquipmentState.PaidScrap);
        Assert.Equal(7750, f.Gear.ProgressionData.EquipmentState.PaidCinders);
    }

    [Fact]
    public async Task Learning_consumes_one_book_and_unlocks_one_free_use_across_all_compatible_items()
    {
        await using var f = await Fixture.Create();
        var book = await f.AddStack(Fury, 2);
        var learn = await f.Preview(ForgeOperationKind.LearnStyle, Fury, book.ItemInstanceId);
        Assert.True((await f.Execute(learn)).Outcome is not null);
        Assert.Equal(1, book.Quantity);
        var duplicate = await f.Preview(ForgeOperationKind.LearnStyle, Fury, book.ItemInstanceId);
        Assert.True(duplicate.IsNoOp);
        await f.Execute(duplicate);
        Assert.Equal(1, book.Quantity);
        var other = await f.AddEquipment("plain.band");
        var first = await f.Preview(ForgeOperationKind.ChangeStyle, Fury);
        var second = await f.Preview(ForgeOperationKind.ChangeStyle, Fury, other.Id);
        Assert.True(first.UsesFreeApplication);
        Assert.True(second.UsesFreeApplication);
        await f.Execute(first);
        Assert.Equal(10000, f.Character.Cinders);
        Assert.Equal("set_fury", f.Gear.ProgressionData!.EquipmentSetId);
        var stale = await f.Execute(second);
        Assert.Null(stale.Outcome);
        Assert.False(stale.FreshQuote!.UsesFreeApplication);
        Assert.Equal(250, stale.FreshQuote.CinderCost);
        await f.Execute(stale.FreshQuote);
        Assert.Equal(9750, f.Character.Cinders);
        Assert.True(other.IsBound);
        f.Db.ChangeTracker.Clear();
        var learned = await f.Db.LearnedEquipmentStyles.SingleAsync();
        Assert.Equal(first.OperationId, learned.FreeApplicationOperationId);
        var options = await f.Service.GetStylesAsync(f.Character.Id, other.Id, Ct);
        var option = Assert.Single(options, x => x.Id == Fury);
        Assert.True(option.IsLearned && option.IsCompatible && option.IsActive);
        Assert.False(option.FreeApplicationAvailable);
        Assert.Equal(13, Mapper().Map<List<ForgeStyleOptionDto>>(options).Count);
    }

    [Fact]
    public async Task Style_noop_is_free_and_plain_restoration_preserves_rank_and_paid_basis()
    {
        await using var f = await Fixture.Create();
        await f.Learn(Fury);
        await f.Execute(await f.Preview(ForgeOperationKind.ImproveRank));
        await f.Execute(await f.Preview(ForgeOperationKind.ChangeStyle, Fury));
        var eventCount = await f.Db.GameEventOutboxMessages.CountAsync();
        var noOp = await f.Preview(ForgeOperationKind.ChangeStyle, Fury);
        Assert.True(noOp.IsNoOp);
        Assert.False(noOp.UsesFreeApplication);
        Assert.Equal(0, noOp.CinderCost);
        await f.Execute(noOp);
        Assert.Equal(eventCount, await f.Db.GameEventOutboxMessages.CountAsync());
        var plain = await f.Preview(ForgeOperationKind.ChangeStyle);
        Assert.True(plain.CanExecute);
        Assert.Equal(250, plain.CinderCost);
        await f.Execute(plain);
        Assert.Null(f.Gear.ProgressionData!.State.ActiveStyleId);
        Assert.Null(f.Gear.ProgressionData.EquipmentSetId);
        Assert.Equal(1, f.Gear.ProgressionData.State.Rank);
        Assert.Equal(5, f.Gear.ProgressionData.EquipmentState.PaidScrap);
        Assert.Equal(250, f.Gear.ProgressionData.EquipmentState.PaidCinders);
        Assert.Equal(9500, f.Character.Cinders);
    }

    [Fact]
    public async Task Unknown_unlearned_incompatible_or_wrong_book_style_requests_do_not_write()
    {
        await using var f = await Fixture.Create();
        Assert.False((await f.Preview(ForgeOperationKind.ChangeStyle, Fury)).CanExecute);
        Assert.False((await f.Preview(ForgeOperationKind.ChangeStyle, "unknown")).CanExecute);
        var book = await f.AddStack("blueprint_arcane", 1);
        Assert.False((await f.Preview(ForgeOperationKind.LearnStyle, Fury, book.ItemInstanceId)).CanExecute);
        await f.Learn(Fury);
        var helm = await f.AddEquipment("plain.heavy_helm");
        var invalid = await f.Preview(ForgeOperationKind.ChangeStyle, Fury, helm.Id);
        Assert.False(invalid.CanExecute);
        Assert.Null((await f.Execute(invalid)).Outcome);
        Assert.Null((await f.Db.LearnedEquipmentStyles.SingleAsync()).FreeApplicationOperationId);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(0, true, 2)]
    [InlineData(2, true, 10)]
    public async Task Salvage_returns_only_actual_paid_scrap_and_replays_after_equipment_deletion(int awardedRank, bool improve, long refund)
    {
        await using var f = await Fixture.Create(awardedRank: awardedRank);
        if (improve) await f.Execute(await f.Preview(ForgeOperationKind.ImproveRank));
        var beforeScrap = await f.Scrap();
        var beforeCinders = f.Character.Cinders;
        var quote = await f.Preview(ForgeOperationKind.Salvage);
        Assert.Equal(refund, quote.ScrapReturned);
        Assert.Null(quote.After);
        var result = await f.Execute(quote);
        Assert.NotNull(result.Outcome);
        Assert.False(await f.Db.ItemInstances.AnyAsync(x => x.Id == f.Gear.Id));
        Assert.Equal(beforeScrap + refund, await f.Scrap());
        f.Db.ChangeTracker.Clear();
        Assert.NotNull((await f.Execute(quote)).Outcome);
        Assert.Equal(beforeScrap + refund, await f.Scrap());
        Assert.Equal(beforeCinders, (await f.Db.Characters.SingleAsync()).Cinders);
        Assert.Equal(refund, await f.Db.EconomyLedger.Where(x => x.ReferenceId == quote.OperationId && x.RecipientCharacterId != null).SumAsync(x => x.Quantity));
    }

    [Fact]
    public async Task Favorite_salvage_requires_override_and_refund_overflow_is_rejected()
    {
        await using var f = await Fixture.Create();
        var inventory = await f.Db.InventoryItems.SingleAsync(x => x.ItemInstanceId == f.Gear.Id);
        inventory.IsFavorite = true;
        await f.Db.SaveChangesAsync();
        var favorite = await f.Preview(ForgeOperationKind.Salvage);
        Assert.False(favorite.CanExecute);
        var allowed = await f.Service.PreviewAsync(f.Character.Id, favorite.Request with { AllowFavoriteSalvage = true }, Ct);
        Assert.True(allowed.CanExecute);
        await f.Execute(await f.Preview(ForgeOperationKind.ImproveRank));
        var scrap = await f.Db.InventoryItems.SingleAsync(x => x.ItemInstance.ItemBaseId == "tempered_scrap");
        scrap.Quantity = int.MaxValue;
        await f.Db.SaveChangesAsync();
        var overflow = await f.Service.PreviewAsync(f.Character.Id, allowed.Request, Ct);
        Assert.False(overflow.CanExecute);
        Assert.Contains("balance", overflow.UnavailableReason);
        Assert.Null((await f.Execute(overflow)).Outcome);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Theory]
    [InlineData("listed")]
    [InlineData("vault")]
    [InlineData("foreign")]
    [InlineData("stacked")]
    public async Task Unavailable_equipment_cannot_be_upgraded_or_destroyed(string location)
    {
        await using var f = await Fixture.Create();
        switch (location)
        {
            case "listed": f.Db.MarketPlaceListings.Add(new() { Id = Guid.NewGuid(), ItemInstanceId = f.Gear.Id }); break;
            case "vault": f.Db.GuildVaultItems.Add(new() { Id = Guid.NewGuid(), EquipmentInstanceId = f.Gear.Id }); break;
            case "foreign":
                f.Gear.ApplyProgressionData(EquipmentData.Create(EquipmentState.Restore(f.Gear.ProgressionData!.State with
                { Ownership = new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()) }), f.Catalog.Evaluator));
                break;
            case "stacked": (await f.Db.InventoryItems.SingleAsync(x => x.ItemInstanceId == f.Gear.Id)).Quantity = 2; break;
        }
        await f.Db.SaveChangesAsync();
        foreach (var kind in new[] { ForgeOperationKind.ImproveRank, ForgeOperationKind.Salvage })
        {
            var quote = await f.Preview(kind);
            Assert.False(quote.CanExecute);
            Assert.Null((await f.Execute(quote)).Outcome);
        }
        Assert.Equal(10000, f.Character.Cinders);
        Assert.Empty(await f.Db.ForgeReceipts.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Equipped_upgrade_settles_old_combat_and_waits_for_backlog_before_charging(bool backlog)
    {
        await using var f = await Fixture.Create();
        await f.Equip(f.Gear);
        var snapshot = new CharacterSnapshot { Id = Guid.NewGuid(), CharacterId = f.Character.Id, Name = "Before", Level = 1,
            Equipment = [EquipmentSnapshot.From(EquipmentSlotType.MainHand, f.Gear)] };
        f.Db.CharacterSnapshots.Add(snapshot);
        await f.Db.SaveChangesAsync();
        var frozen = snapshot.Equipment.Single().ProgressionData!.Serialize();
        f.Actions.Action = new CharacterAction { CharacterId = f.Character.Id, ActionDetails = new CombatActionDetails(), ProcessedCount = 1, HasMoreDueWork = backlog };
        f.Actions.OnResolve = () => { Assert.Equal(0, f.Gear.ProgressionData!.State.Rank); f.Character.Cinders += 10; };
        var quote = await f.Preview(ForgeOperationKind.ImproveRank);
        Assert.NotNull(quote.EquippedImpact);
        AttributeCalculator.CalculateBaseAttributes(f.Character);
        Assert.Equal(f.Character.BaseCombatAttributes.OrderBy(x => x.Key), quote.EquippedImpact.BeforeAttributes.OrderBy(x => x.Key));
        Assert.NotEqual(quote.EquippedImpact.BeforeAttributes[AttributeType.Power], quote.EquippedImpact.AfterAttributes[AttributeType.Power]);
        Assert.NotNull(Mapper().Map<ForgeQuoteDto>(quote).EquippedImpact);
        Assert.Equal(0, f.Actions.ResolveCalls);
        var result = await f.Execute(quote);
        Assert.Equal(1, f.Actions.ResolveCalls);
        Assert.True(f.Sync.Invalidations > 0);
        Assert.Equal(backlog ? 0 : 1, f.Gear.ProgressionData!.State.Rank);
        Assert.Equal(backlog ? 10010 : 9760, f.Character.Cinders);
        Assert.Equal(backlog, result.Outcome is null);
        Assert.Equal(frozen, snapshot.Equipment.Single().ProgressionData!.Serialize());
        var events = await f.Db.GameEventOutboxMessages.Select(x => x.EventType).ToListAsync();
        if (backlog) Assert.Empty(events);
        else Assert.Equal(new[] { GameEventTypes.EquipmentChanged, GameEventTypes.ForgeCompleted }.Order(), events.Order());
        Assert.False((await f.Preview(ForgeOperationKind.Salvage)).CanExecute);
    }

    [Fact]
    public async Task Equipped_style_preview_removes_set_threshold_and_changed_loadout_invalidates_quote()
    {
        await using var f = await Fixture.Create();
        await f.Learn(Fury);
        var band = await f.AddEquipment("plain.band");
        await f.Execute(await f.Preview(ForgeOperationKind.ChangeStyle, Fury));
        await f.Execute(await f.Preview(ForgeOperationKind.ChangeStyle, Fury, band.Id));
        await f.Equip(f.Gear);
        await f.Equip(band);
        var quote = await f.Preview(ForgeOperationKind.ChangeStyle);
        Assert.NotEmpty(quote.EquippedImpact!.BeforeSetBonusIds);
        Assert.Empty(quote.EquippedImpact.AfterSetBonusIds);
        f.Character.Level++;
        await f.Db.SaveChangesAsync();
        var result = await f.Execute(quote);
        Assert.Null(result.Outcome);
        Assert.NotNull(result.FreshQuote);
        Assert.Equal(Fury, f.Gear.ProgressionData!.State.ActiveStyleId);
        Assert.Equal(9750, f.Character.Cinders);
    }

    [Fact]
    public async Task Separate_command_contexts_serialize_competing_quotes_and_only_one_upgrade_commits()
    {
        await using var f = await Fixture.Create();
        var first = await f.Preview(ForgeOperationKind.ImproveRank);
        var second = await f.Preview(ForgeOperationKind.ImproveRank);
        async Task<Response<ForgeMutationDto>> Run(ForgeQuote quote)
        {
            await using var db = new LLDbContext(f.DbOptions);
            var request = new ImproveEquipmentProgressionRankCommand(f.Character.Id, quote.OperationId, f.Gear.Id, quote.Token);
            var handler = new ImproveEquipmentProgressionRankCommandHandler(f.ServiceWith(db: db), Mapper());
            var pipeline = new TransactionBehavior<ImproveEquipmentProgressionRankCommand, Response<ForgeMutationDto>>(db, f.Sync,
                NullLogger<TransactionBehavior<ImproveEquipmentProgressionRankCommand, Response<ForgeMutationDto>>>.Instance);
            return await pipeline.Handle(request, ct => handler.Handle(request, ct), Ct);
        }
        var results = await Task.WhenAll(Run(first), Run(second));
        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsConflict);
        f.Db.ChangeTracker.Clear();
        Assert.Single(await f.Db.ForgeReceipts.ToListAsync());
        Assert.Equal(9750, (await f.Db.Characters.SingleAsync()).Cinders);
        Assert.Equal(95, await f.Scrap());
        Assert.Equal(1, (await f.Db.ItemInstances.OfType<EquipmentInstance>().SingleAsync()).ProgressionData!.State.Rank);
    }

    [Fact]
    public async Task Disabled_forge_and_foreign_character_requests_are_read_only()
    {
        await using var f = await Fixture.Create();
        var disabled = f.ServiceWith(enabled: false);
        var request = new ForgeRequest(ForgeOperationKind.ImproveRank, f.Gear.Id);
        var quote = await disabled.PreviewAsync(f.Character.Id, request, Ct);
        Assert.False(quote.CanExecute);
        Assert.Empty(await disabled.GetStylesAsync(f.Character.Id, f.Gear.Id, Ct));
        Assert.Null((await disabled.ExecuteAsync(f.Character.Id, quote.OperationId, request, quote.Token, Ct)).Outcome);
        Assert.False((await f.Service.PreviewAsync(Guid.NewGuid(), request, Ct)).CanExecute);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Receipt_json_preserves_results_and_is_independent_of_item_lifetime()
    {
        await using var f = await Fixture.Create();
        var result = await f.Execute(await f.Preview(ForgeOperationKind.ImproveRank));
        using var metadata = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata_only;Username=unused").Options);
        var entity = metadata.Model.FindEntityType(typeof(ForgeReceipt))!;
        Assert.Equal(new[] { "CharacterId", "OperationId" }, entity.FindPrimaryKey()!.Properties.Select(x => x.Name));
        Assert.DoesNotContain(entity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(ItemInstance));
        var property = entity.FindProperty(nameof(ForgeReceipt.Outcome))!;
        Assert.Equal("jsonb", property.GetColumnType());
        var converter = property.GetTypeMapping().Converter!;
        var restored = Assert.IsType<ForgeOutcome>(converter.ConvertFromProvider(converter.ConvertToProvider(result.Outcome)));
        Assert.Equal(result.Outcome!.After!.Serialize(), restored.After!.Serialize());
        Assert.Equal(5, restored.After.EquipmentState.PaidScrap);
        Assert.True(metadata.Model.FindEntityType(typeof(LearnedEquipmentStyle))!
            .FindProperty(nameof(LearnedEquipmentStyle.FreeApplicationOperationId))!.IsConcurrencyToken);
    }

    private static IMapper Mapper() => new MapperConfiguration(x => x.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Native_restoration_costs_the_normal_fee_and_does_not_spend_learned_free_use()
    {
        await using var f = await Fixture.Create();
        var definitions = f.Catalog.Options.Select(x => f.Catalog.Evaluator.GetDefinition(x.DefinitionId)).ToArray();
        var archetypes = definitions.Select(x => f.Catalog.Evaluator.Evaluate(x.Id, 1, 0, null).Archetype).ToArray();
        var native = new EquipmentDefinition("named.staff", "Named staff", "plain.staff", EquipmentRarity.Rare, Fury);
        var evaluator = new EquipmentEvaluator(f.Catalog.Evaluator.Balance, archetypes, f.Catalog.Styles.Select(x => x.Style), definitions.Append(native));
        var catalog = new StarterEquipmentCatalog(evaluator, definitions.Select(x => x.Id), f.Catalog.Styles);
        var state = EquipmentState.Award(f.Gear.Id, evaluator, native.Id, 1, 0,
            new(EquipmentAwardKind.RandomDiscovery, "test", "native"), new(EquipmentOwnershipKind.UnboundPersonal, f.Character.Id));
        f.Gear.ApplyProgressionData(EquipmentData.Create(state.ChangeStyle(evaluator, null, new HashSet<string>()), evaluator));
        await f.Db.SaveChangesAsync();
        var learned = new LearnedEquipmentStyle { CharacterId = f.Character.Id, StyleId = Fury, LearnedAtUtc = f.Clock.Now };
        f.Db.LearnedEquipmentStyles.Add(learned);
        await f.Db.SaveChangesAsync();
        var context = await new ForgeRepository(f.Db).LoadAsync(f.Character.Id, f.Gear.Id, false, Ct);
        var policy = new ForgePolicy(catalog, new(1, [new(1, [5, 10, 20, 40, 80], [250, 500, 1000, 2000, 4000], 250)], .5m));
        var quote = policy.Quote(context, new(ForgeOperationKind.ChangeStyle, f.Gear.Id, Fury), Guid.NewGuid(), f.Clock.Now);
        Assert.True(quote.CanExecute, quote.UnavailableReason);
        Assert.Equal(250, quote.CinderCost);
        Assert.False(quote.UsesFreeApplication);
        Assert.Equal(Fury, quote.After!.State.ActiveStyleId);
        Assert.Null(learned.FreeApplicationOperationId);
        var withoutKnowledge = policy.Quote(context! with { LearnedStyles = [] }, quote.Request, Guid.NewGuid(), f.Clock.Now);
        Assert.True(withoutKnowledge.CanExecute);
        Assert.Equal(250, withoutKnowledge.CinderCost);
    }

    [Fact]
    public async Task Runtime_styles_preserve_authored_compatibility_and_work_through_rank_five()
    {
        await using var f = await Fixture.Create();
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        json.Converters.Add(new JsonStringEnumConverter());
        var reference = new JsonCraftingDefinitionProvider(new ConfigurationBuilder().Build(), ApiRoot(), json);
        foreach (var style in f.Catalog.Styles)
        {
            var blueprint = reference.GetBlueprint(style.Id)!;
            Assert.Equal(blueprint.ItemId, style.ItemBaseId);
            Assert.Equal(blueprint.EquipmentSetId, style.Style.EquipmentSetId);
            var expected = reference.GetRecipes().Where(recipe => EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint))
                .Select(recipe => "plain." + recipe.OutputItemId.ToLowerInvariant()).Order().ToArray();
            Assert.Equal(expected, style.Style.CompatibleArchetypeIds.Order());
            foreach (var id in style.Style.CompatibleArchetypeIds)
            {
                var state = EquipmentState.Award(Guid.NewGuid(), f.Catalog.Evaluator, id, 1, 0,
                    new(EquipmentAwardKind.RandomDiscovery, "test", id), new(EquipmentOwnershipKind.UnboundPersonal, f.Character.Id))
                    .ChangeStyle(f.Catalog.Evaluator, style.Id, new HashSet<string> { style.Id });
                for (var rank = 1; rank <= 5; rank++)
                    state = state.RecordPaidRankImprovement(f.Catalog.Evaluator, Guid.NewGuid(), 1, 1);
                Assert.Equal(5, state.Rank);
                Assert.Equal(style.Style.EquipmentSetId, EquipmentData.Create(state, f.Catalog.Evaluator).EquipmentSetId);
            }
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public DbContextOptions<LLDbContext> DbOptions { get; } = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        public LLDbContext Db { get; }
        public StarterEquipmentCatalog Catalog { get; } = JsonStarterEquipmentCatalog.Load(Path.Combine(ApiRoot(), "Data/equipment/equipment-starters.v1.json"));
        public TestClock Clock { get; } = new();
        public Actions Actions { get; } = new();
        public Sync Sync { get; } = new();
        public Character Character { get; private set; } = null!;
        public EquipmentInstance Gear { get; private set; } = null!;
        public ForgeService Service => ServiceWith();
        public Fixture() => Db = new LLDbContext(DbOptions);

        public static async Task<Fixture> Create(int scrap = 100, long cinders = 10000, int awardedRank = 0, int tier = 1)
        {
            var f = new Fixture();
            var id = Guid.NewGuid();
            f.Character = new Character { Id = id, UserId = Guid.NewGuid(), Name = "Forge", NormalizedName = "FORGE", Level = 1,
                Cinders = cinders, Inventory = new Inventory { CharacterId = id }, EquipmentSlots = Enum.GetValues<EquipmentSlotType>()
                    .Select(x => new EquipmentSlot { EntityId = id, EquipmentSlotType = x }).ToList() };
            f.Db.Characters.Add(f.Character);
            foreach (var option in f.Catalog.Options)
            {
                var data = f.Catalog.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
                f.Db.ItemBases.Add(new EquipmentBase { Id = data.Archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType });
            }
            f.Db.ItemBases.Add(new ItemBase { Id = "tempered_scrap", Name = "Tempered Scrap", Stackable = true });
            foreach (var style in f.Catalog.Styles) f.Db.ItemBases.Add(new ItemBase { Id = style.ItemBaseId, Name = style.Name, Stackable = true });
            await f.Db.SaveChangesAsync();
            f.Gear = await f.AddEquipment("plain.staff", awardedRank, tier);
            if (scrap > 0) await f.AddStack("tempered_scrap", scrap);
            return f;
        }

        public ForgeService ServiceWith(ForgePrices? prices = null, LLDbContext? db = null, bool enabled = true)
        {
            db ??= Db;
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            json.Converters.Add(new JsonStringEnumConverter());
            return new(Catalog, prices ?? JsonStarterEquipmentCatalog.LoadForgePrices(Path.Combine(ApiRoot(), "Data/equipment/equipment-forge-prices.v1.json")),
                new ForgeRepository(db), Actions, new GameEventOutbox(db, new GameEventOutboxConsumerRegistry(), json, Clock),
                Options.Create(new EquipmentProgressionOptions { ForgeEnabled = enabled }), Clock, Sync, new Essences(),
                new JsonCraftingDefinitionProvider(new ConfigurationBuilder().Build(), ApiRoot(), json));
        }

        public Task<ForgeQuote> Preview(ForgeOperationKind kind, string? style = null, Guid? itemId = null) =>
            Service.PreviewAsync(Character.Id, new(kind, itemId ?? Gear.Id, style), Ct);
        public async Task<ForgeResult> Execute(ForgeQuote quote)
        {
            var result = await Service.ExecuteAsync(Character.Id, quote.OperationId, quote.Request, quote.Token, Ct);
            await Db.SaveChangesAsync();
            return result;
        }
        public async Task Learn(string style)
        {
            var book = await AddStack(style, 1);
            Assert.NotNull((await Execute(await Preview(ForgeOperationKind.LearnStyle, style, book.ItemInstanceId))).Outcome);
        }
        public async Task<InventoryItem> AddStack(string itemBaseId, int quantity)
        {
            var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = itemBaseId, ItemBase = await Db.ItemBases.SingleAsync(x => x.Id == itemBaseId) };
            var item = new InventoryItem { InventoryId = Character.Id, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = quantity };
            Db.InventoryItems.Add(item);
            await Db.SaveChangesAsync();
            return item;
        }
        public async Task<EquipmentInstance> AddEquipment(string definition, int rank = 0, int tier = 1)
        {
            var state = EquipmentState.Award(Guid.NewGuid(), Catalog.Evaluator, definition, tier, rank,
                new(EquipmentAwardKind.RandomDiscovery, "test", Guid.NewGuid().ToString()), new(EquipmentOwnershipKind.UnboundPersonal, Character.Id));
            var data = EquipmentData.Create(state, Catalog.Evaluator);
            var equipment = new EquipmentInstance { Id = state.Id, ItemBaseId = data.ItemBaseId, ItemBase = await Db.ItemBases.SingleAsync(x => x.Id == data.ItemBaseId) };
            equipment.ApplyProgressionData(data);
            Db.InventoryItems.Add(new() { InventoryId = Character.Id, ItemInstanceId = equipment.Id, ItemInstance = equipment, Quantity = 1 });
            await Db.SaveChangesAsync();
            return equipment;
        }
        public async Task Equip(EquipmentInstance equipment)
        {
            Assert.True((await new EquipmentSlotRepository(Db).EquipEquipmentAsync(Character.Id, equipment.Id, null, Ct)).Succeeded);
            await Db.SaveChangesAsync();
        }
        public Task<int> Scrap() => Db.InventoryItems.Where(x => x.InventoryId == Character.Id && x.ItemInstance.ItemBaseId == "tempered_scrap").SumAsync(x => x.Quantity);
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 2, 12, 1, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Essences : IEssenceCombatLoadoutResolver
    {
        public EssenceCombatLoadout Resolve(Guid id, IEnumerable<PlayerEssence> essences) => new(id, essences.ToArray(), [], new HashSet<string>());
        public Task<EssenceCombatLoadout> ResolveAsync(Guid id, CancellationToken ct) => Task.FromResult(Resolve(id, []));
    }
    private sealed class Actions : ICharacterActionService
    {
        public CharacterAction? Action { get; set; }
        public Action? OnResolve { get; set; }
        public int ResolveCalls { get; private set; }
        public Task<CharacterAction?> PeekCharacterActionAsync(Guid id, CancellationToken ct) => Task.FromResult(Action);
        public Task<CharacterAction?> GetCharacterActionAsync(Guid id, CancellationToken ct) { ResolveCalls++; OnResolve?.Invoke(); return Task.FromResult(Action); }
        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction action, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteCharacterActionAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Sync : IStateSyncService
    {
        public int Invalidations { get; private set; }
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? id) => new Dictionary<string, long>();
        public Task InvalidateCharacterAsync(Guid id, string reason, CancellationToken ct = default) { Invalidations++; return Task.CompletedTask; }
        public Task InvalidateCharacterScopeAsync(Guid id, string scope, string reason, CancellationToken ct = default) { Invalidations++; return Task.CompletedTask; }
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private static string ApiRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "LL/src/API/API.LL");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException("API content was not found.");
    }
}
