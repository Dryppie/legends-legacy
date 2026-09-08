using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Items.Dtos;
using AutoMapper;
using Domain.Models.Items;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class SelectionCrateMetadataDtoMappingTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMapper _mapper;

    public SelectionCrateMetadataDtoMappingTests()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var definitions = new JsonEssenceDefinitionRepository(
            new ConfigurationBuilder().Build(),
            TestContentPaths.FindApiRoot(),
            options,
            new EssenceDefinitionValidator());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEssenceDefinitionRepository>(definitions);
        services.AddApplication();
        _provider = services.BuildServiceProvider();
        _mapper = _provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void Every_essence_token_option_includes_the_reward_essences_ability_details()
    {
        foreach (var token in ShenicEssenceTokenCatalog.Definitions)
        {
            var item = _mapper.Map<ItemBaseDto>(new ItemBase { Id = token.ItemBaseId });

            Assert.NotNull(item.SelectionCrate);
            Assert.Equal(token.Options.Count, item.SelectionCrate.Options.Count);
            foreach (var reward in token.Options)
            {
                var option = Assert.Single(item.SelectionCrate.Options, option => option.Id == reward.Id);
                Assert.Equal(reward.Name, option.Name);
                Assert.Equal(reward.Quantity, option.Quantity);
                Assert.NotNull(option.Essence);
                Assert.Equal(reward.ItemId, $"item.{option.Essence.Id}");
                Assert.False(string.IsNullOrWhiteSpace(option.Essence.DisplayName));
                Assert.False(string.IsNullOrWhiteSpace(option.Essence.ActiveAbility.Name));
                Assert.False(string.IsNullOrWhiteSpace(option.Essence.ActiveAbility.Description));
                Assert.NotEmpty(option.Essence.ActiveAbility.Effects);
                Assert.False(string.IsNullOrWhiteSpace(option.Essence.PassiveAbility.Name));
                Assert.False(string.IsNullOrWhiteSpace(option.Essence.PassiveAbility.Description));
            }
        }
    }

    [Theory]
    [InlineData(TutorialArmsChestCatalog.ItemBaseId)]
    [InlineData(LegacyBlueprintSelectionBoxCatalog.ItemBaseId)]
    public void Other_selection_containers_keep_their_choices_without_essence_details(string itemId)
    {
        var item = _mapper.Map<ItemBaseDto>(new ItemBase { Id = itemId });

        Assert.NotNull(item.SelectionCrate);
        Assert.NotEmpty(item.SelectionCrate.Options);
        Assert.All(item.SelectionCrate.Options, option => Assert.Null(option.Essence));
    }

    public void Dispose() => _provider.Dispose();
}
