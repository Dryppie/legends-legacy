using Application.UseCases._AdminDashboard.Items.Dtos;
using Common.Utilities.EnumConverters;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Services.AdminDashboard.Converters;
using Services.AdminDashboard.Items;
using Services.AdminDashboard.Recipes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.AdminDashboard.JsonReaders;
public class RecipeJsonReader
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _opts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new SafeEnumConverter<AttributeType>(),
                new FallbackEnumConverter<EquipmentType>(EquipmentType.Head), new JsonStringEnumConverter() } };

    private List<RecipeToJsonDto> _cache = [];

    public RecipeJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;
        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "recipes.json");

        LoadFile();
    }

    public List<Recipe> GetRecipes() =>
        [.. _cache.Select(r => r.ToEntity())];

    public void AddRecipe(Recipe recipe)
    {
        _cache.Add(recipe.ToDto());
        SaveFile();
    }

    public void UpdateRecipe(Recipe recipe)
    {
        var idx = _cache.FindIndex(r => r.Id == recipe.Id);

        if (idx == -1) _cache.Add(recipe.ToDto());
        else _cache[idx] = recipe.ToDto();

        SaveFile();
    }

    public void RemoveRecipe(Guid id)
    {
        _cache.RemoveAll(r => r.Id == id);
        SaveFile();
    }

    private void LoadFile()
    {
        if (!File.Exists(_filePath))
        {
            _cache = [];
            return;
        }

        var json = File.ReadAllText(_filePath);
        _cache = JsonSerializer.Deserialize<List<RecipeToJsonDto>>(json, _opts) ?? [];

        var itemReader = new ItemBaseJsonReader();
        var itemDtos = itemReader.GetItemsFromJson();
        var items = new List<ItemBase>();
        foreach (var itemDto in itemDtos)
        {
            var item = new ItemBase();
            item = itemDto.ItemType switch
            {
                ItemType.Equipment => EquipmentBase(itemDto),
                _ => new ItemBase
                {
                    Id = itemDto.Id,
                    Name = itemDto.Name,
                    Description = itemDto.Description,
                    ItemType = itemDto.ItemType,
                    Rarity = itemDto.Rarity
                }
            };
            items.Add(item);
        }

        foreach (var recipe in _cache)
        {
            recipe.Item = (items.FirstOrDefault(i => i.Id == recipe.ItemId) as EquipmentBase)!.ToDto();
            recipe.Materials = recipe.Materials
                .Select(m => new Material
                {
                    ItemId = m.ItemId,
                    Item = items.FirstOrDefault(i => i.Id == m.ItemId)!,
                    Quantity = m.Quantity
                })
                .ToList();
        }
        SaveFile();
    }

    private static EquipmentBase EquipmentBase(ItemBaseDto itemDto)
    {
        return new EquipmentBase()
        {
            Id = itemDto.Id,
            Name = itemDto.Name,
            Description = itemDto.Description,
            ItemType = itemDto.ItemType,
            Rarity = itemDto.Rarity,
            EquipmentType = itemDto.EquipmentType,
            AttributeModifiers = itemDto.AttributeModifiers,
            AttackSpeed = itemDto.AttackSpeed,
            Magnitude = itemDto.Magnitude,
            MagnitudeRange = itemDto.MagnitudeRange,
            GatheringType = itemDto.GatheringType,
            ScalingAttribute = itemDto.ScalingAttribute,
            ScalingAmount = itemDto.ScalingAmount
        };
    }

    private void SaveFile() =>
        File.WriteAllText(_filePath,
            JsonSerializer.Serialize(_cache, _opts));
}
