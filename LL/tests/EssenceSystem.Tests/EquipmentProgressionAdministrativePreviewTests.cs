using API.LiveOps.Previews;
using API.LiveOps.Controllers;
using Application.UseCases.Administration;
using Domain.Models.Administration;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.AspNetCore.Authorization;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsActionPreviewTests
{
    [Fact]
    public async Task EquipmentProgression_preview_displays_exact_award_and_rejects_changed_parameters_or_catalog()
    {
        var fixture = CreateFixture();
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(), "Data/equipment/equipment-starters.v1.json"));
        var operation = Guid.NewGuid();
        var request = new EquipmentGrantRequest("plain.shortsword", 1, 3, "blueprint_fury");
        var data = EquipmentProgressionAdministrativeEquipment.Create(catalog.Evaluator, request, fixture.Player.CharacterId, Guid.NewGuid(), operation);
        var item = new ItemBase { Id = "shortsword", Name = "Shortsword", ItemType = ItemType.Equipment, Stackable = false };
        fixture.LiveOps.Item = new("shortsword", "Shortsword", "Sword", ItemType.Equipment, Rarity.Common, false, false);
        fixture.LiveOps.GrantPlan = new(item, true, data);
        var result = await fixture.Service.CreateCompensationGrantAsync(operation, fixture.Player.CharacterId, fixture.Actor,
            "shortsword", 1, "CASE-Progression", null, CancellationToken.None, request);
        var preview = Assert.IsType<ActionPreviewDto>(result.Data);
        Assert.Equal("HighValue", preview.RiskLevel);
        Assert.Contains(preview.Fields, f => f.Label == "Behavior" && f.Value.Contains("Bound to recipient"));
        Assert.Contains(preview.Fields, f => f.Label == "Salvage" && f.Value.Contains("0 base Scrap"));
        Assert.Contains(preview.Fields, f => f.Label == "Tier / Rank" && f.Value == "1 / 3");
        foreach (var changed in new[] { request with { Rank = 4 }, request with { Tier = 2 }, request with { ActiveStyleId = null }, request with { DefinitionId = "plain.dagger" } })
        {
            var mismatch = await fixture.Service.BeginCompensationGrantAsync(preview.PreviewToken, operation, fixture.Player.CharacterId,
                fixture.Actor, "shortsword", 1, "CASE-Progression", null, CancellationToken.None, changed);
            Assert.False(mismatch.IsSuccess);
            Assert.True(mismatch.IsConflict);
        }
        Assert.True((await fixture.Service.BeginCompensationGrantAsync(preview.PreviewToken, operation, fixture.Player.CharacterId,
            fixture.Actor, "shortsword", 1, "CASE-Progression", null, CancellationToken.None, request)).IsSuccess);
        fixture.LiveOps.GrantPlan = new(item, false, null);
        var staleRetry = await fixture.Service.BeginCompensationGrantAsync(preview.PreviewToken, operation, fixture.Player.CharacterId,
            fixture.Actor, "shortsword", 1, "CASE-Progression", null, CancellationToken.None, request);
        Assert.False(staleRetry.IsSuccess);
        Assert.True(staleRetry.IsConflict);
    }

    [Fact]
    public void EquipmentProgression_compensation_read_preview_and_write_endpoints_require_compensation_permission()
    {
        foreach (var method in new[] { nameof(CompensationController.EquipmentOptions), nameof(CompensationController.PreviewGrantItems), nameof(CompensationController.GrantItems) })
        {
            var attribute = Assert.Single(typeof(CompensationController).GetMethod(method)!.GetCustomAttributes(typeof(AuthorizeAttribute), true));
            Assert.Equal(AdministrationPermissions.EconomyCompensation, Assert.IsType<AuthorizeAttribute>(attribute).Policy);
        }
    }
}
