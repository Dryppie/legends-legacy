using System.Data.Common;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed partial class GuildBuildingServiceTests
{
    [Theory]
    [InlineData("overview")]
    [InlineData("construct")]
    [InlineData("upgrade")]
    [InlineData("set-target")]
    public async Task Building_lookup_compiles_without_multiple_collection_warnings(string operation)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql("Host=localhost;Database=query_compilation_only;Username=unused")
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new StopBeforeDatabaseConnection())
            .Options;
        await using var context = new LLDbContext(options);
        var service = CreateService(context);
        var characterId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        // Each entry point compiles the shared building lookup before opening a connection.
        await Assert.ThrowsAsync<QueryCompiledException>(async () =>
        {
            switch (operation)
            {
                case "overview":
                    await service.GetOverviewAsync(characterId, now, CancellationToken.None);
                    break;
                case "construct":
                    await service.ConstructAsync(characterId, GuildBuildingType.MissionBoard, now, CancellationToken.None);
                    break;
                case "upgrade":
                    await service.UpgradeAsync(characterId, Guid.NewGuid(), now, CancellationToken.None);
                    break;
                case "set-target":
                    await service.SetCurrentTargetAsync(characterId, GuildBuildingType.MissionBoard, now, CancellationToken.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });
    }

    private sealed class QueryCompiledException : Exception;

    private sealed class StopBeforeDatabaseConnection : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result) => throw new QueryCompiledException();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default) => throw new QueryCompiledException();
    }
}
