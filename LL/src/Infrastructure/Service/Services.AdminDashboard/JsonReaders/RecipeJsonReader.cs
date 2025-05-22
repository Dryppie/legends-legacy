using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases._AdminDashboard.Items.Dtos;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;

namespace Services.AdminDashboard.JsonReaders;
public class RecipeJsonReader
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _opts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };

    private List<Recipe> _cache = [];

    public RecipeJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;

        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "recipes.json");

        LoadFile();
    }

    public List<Recipe> GetRecipes() =>
        [.. _cache];

    public void AddRecipe(Recipe recipe)
    {
        _cache.Add(recipe);
        SaveFile();
    }

    public void UpdateRecipe(Recipe recipe)
    {
        var idx = _cache.FindIndex(r => r.Id == recipe.Id);

        if (idx == -1) _cache.Add(recipe);
        else _cache[idx] = recipe;

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
        _cache = JsonSerializer.Deserialize<List<Recipe>>(json, _opts) ?? [];

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
            recipe.Item = items.FirstOrDefault(i => i.Id == recipe.ItemId)!;
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

    private EquipmentBase EquipmentBase(ItemBaseDto itemDto)
    {
        return new EquipmentBase()
        {
            Id = itemDto.Id,
            Name = itemDto.Name,
            Description = itemDto.Description,
            ItemType = itemDto.ItemType,
            Rarity = itemDto.Rarity,
            EquipmentType = itemDto.EquipmentType,
            AttributeModifiers = itemDto.AttributeModifiers
        };
    }

    private void SaveFile() =>
        File.WriteAllText(_filePath,
            JsonSerializer.Serialize(_cache, _opts));
}