using API.LiveOps.Previews;
using Domain.Models.Users;
using Domain.Models.Administration;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Repositories.Administration;
using Services.LL.Administration;
namespace EssenceSystem.Tests;

public sealed partial class LiveOpsActionPreviewTests
{
    [Fact]
    public async Task Package_preview_expands_saved_items_and_rejects_a_definition_changed_before_submission()
    {
        var f = CreateFixture(); await using var db = f.Factory.CreateDbContext(); var packageId = Guid.NewGuid();
        db.Set<CompensationPackageVersion>().Add(new CompensationPackageVersion { Id = Guid.NewGuid(), PackageId = packageId,
            Version = 1, Name = "Potion replacement", Purpose = "Verified support issue", ItemsJson = "[{\"ItemBaseId\":\"healing-potion\",\"Quantity\":2}]",
            ActorSubject = f.Actor.Subject, RequestHash = "fixture", CreatedAt = Now }); await db.SaveChangesAsync();
        var packages = new CompensationPackageService(new CompensationPackageRepository(db), new AdministrationRepository(db), f.LiveOps,
            Options.Create(new LiveOpsOptions()), f.Time);
        var service = new LiveOpsActionPreviewService(f.Factory, f.LiveOps, new TestChatGateway(), Options.Create(new LiveOpsOptions()), f.Time, packages);
        var operationId = Guid.NewGuid(); var response = await service.CreatePackageGrantAsync(operationId, f.Player.CharacterId, packageId, 1,
            f.Actor, "Verified missing potion", null, default);
        Assert.True(response.IsSuccess); var preview = response.Data!;
        Assert.Contains(preview.Fields, field => field.Value.Contains("2") && field.Value.Contains("Healing Potion"));
        Assert.Equal(f.Player.CharacterName, preview.ConfirmationText);
        db.Set<CompensationPackageVersion>().Add(new CompensationPackageVersion { Id = Guid.NewGuid(), PackageId = packageId,
            Version = 2, Archived = true, Name = "Potion replacement", Purpose = "Retired", ItemsJson = "[]",
            ActorSubject = f.Actor.Subject, RequestHash = "fixture-archive", CreatedAt = Now }); await db.SaveChangesAsync();
        var submission = await service.BeginPackageGrantAsync(preview.PreviewToken, operationId, f.Player.CharacterId, packageId, 1,
            f.Actor, "Verified missing potion", null, default);
        Assert.False(submission.IsSuccess);
    }

    [Fact]
    public async Task Temporary_preview_uses_server_time_and_submission_binds_the_exact_returned_expiry()
    {
        var f = CreateFixture(); var id = Guid.NewGuid();
        var response = await f.Service.CreateAccountBanAsync(id, f.Player.AccountId, f.Actor, "Case", null, null, default, durationMinutes: 60);
        Assert.True(response.IsSuccess); var p = response.Data!;
        Assert.Equal(Now.AddHours(1), p.EffectExpiresAt); Assert.Equal(Now, p.ServerTimeUtc);
        Assert.True((await f.Service.BeginAccountBanAsync(p.PreviewToken, id, f.Player.AccountId, f.Actor, "Case", null, p.EffectExpiresAt, default)).IsSuccess);
        Assert.False((await f.Service.BeginAccountBanAsync(p.PreviewToken, id, f.Player.AccountId, f.Actor, "Case", null, Now.AddHours(2), default)).IsSuccess);
        Assert.False((await f.Service.CreateAccountBanAsync(Guid.NewGuid(), f.Player.AccountId, f.Actor, "Case", null, null, default, durationMinutes: -1)).IsSuccess);
    }

    [Fact]
    public async Task Signet_preview_requires_registered_recipient_and_binds_quantity_and_actor()
    {
        var f = CreateFixture(); var user = AppUser.Register("SignetTarget", "signets@example.test", "fixture");
        f.LiveOps.Player = f.Player with { AccountId = user.Id };
        Assert.False((await f.Service.CreateAlphaSignetGrantAsync(Guid.NewGuid(), f.Player.CharacterId, f.Actor, 1, "Case grant", default)).IsSuccess);
        await using (var db = f.Factory.CreateDbContext()) { db.Users.Add(user); await db.SaveChangesAsync(); }
        var id = Guid.NewGuid(); var result = await f.Service.CreateAlphaSignetGrantAsync(id, f.Player.CharacterId, f.Actor, 2, "Case grant", default);
        Assert.True(result.IsSuccess); var p = result.Data!;
        Assert.Equal("HighValue", p.RiskLevel); Assert.Equal(f.Player.CharacterName, p.ConfirmationText);
        Assert.False((await f.Service.BeginAlphaSignetGrantAsync(p.PreviewToken, id, f.Player.CharacterId, f.Actor, 3, "Case grant", default)).IsSuccess);
        Assert.False((await f.Service.BeginAlphaSignetGrantAsync(p.PreviewToken, id, f.Player.CharacterId, f.Actor with { Subject = "another" }, 2, "Case grant", default)).IsSuccess);
        Assert.True((await f.Service.BeginAlphaSignetGrantAsync(p.PreviewToken, id, f.Player.CharacterId, f.Actor, 2, "Case grant", default)).IsSuccess);
    }
}
