using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.MediatR.Synchronization;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.Essences.Commands.SpendEssenceDust;
using Application.UseCases.MarketPlaces.Commands.BuyCommodity;
using Application.WebSockets.Contracts;

namespace EssenceSystem.Tests;

public sealed class StateSyncCommandScopeCatalogTests
{
    [Fact]
    public void SpecializedTransactionalCommandsRequireExplicitScopeRegistration()
    {
        var missingCommands = typeof(ICommandBase).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ICommandBase).IsAssignableFrom(type))
            .Where(StateSyncCommandScopeCatalog.RequiresExplicitRegistration)
            .Where(type => !Attribute.IsDefined(type, typeof(NonTransactionalAttribute)))
            .Where(type => !StateSyncCommandScopeCatalog.IsExplicitlyRegistered(type))
            .Select(type => type.FullName)
            .Order()
            .ToArray();

        Assert.True(
            missingCommands.Length == 0,
            $"Commands missing an explicit state-sync scope contract:{Environment.NewLine}{string.Join(Environment.NewLine, missingCommands)}");
    }

    [Fact]
    public void MarketplaceCommandDeclaresCharacterAndWorldDependencies()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(BuyCommodityCommand));

        Assert.Contains(StateSyncScopes.Inventory, profile.CharacterScopes);
        Assert.Contains(StateSyncScopes.Marketplace, profile.WorldScopes);
    }

    [Fact]
    public void CharacterActionResolutionRefreshesSummaryOnlyWhenCharacterChanged()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(ResolveCharacterActionCommand));

        Assert.True(profile.RefreshCharacterSummaryWhenChanged);
    }

    [Fact]
    public void SpendingEssenceDustInvalidatesOnlyItsChangedResources()
    {
        var profile = StateSyncCommandScopeCatalog.GetProfile(typeof(SpendEssenceDustCommand));

        Assert.Equal(
            [StateSyncScopes.Essences, StateSyncScopes.Inventory],
            profile.CharacterScopes);
        Assert.Empty(profile.WorldScopes);
        Assert.True(profile.RefreshCharacterOverview);
        Assert.True(profile.RefreshCharacterSummaryWhenChanged);
    }
}
