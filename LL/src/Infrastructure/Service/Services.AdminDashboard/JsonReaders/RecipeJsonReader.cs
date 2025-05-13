using System.Text.Json;
using Domain.Models.Professions.Crafting;
using Services.AdminDashboard.Recipes.Dtos;

namespace Services.AdminDashboard.JsonReaders;
public class RecipeJsonReader
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _opts =
        new() { WriteIndented = true };

    private List<RecipeDto> _cache = [];

    public RecipeJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;

        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "recipes.json");

        LoadFile();
    }

    public List<Recipe> GetRecipes() =>
        [.. _cache.Select(dto => dto.ToEntity())];

    public void AddRecipe(Recipe recipe)
    {
        _cache.Add(recipe.ToDto());
        SaveFile();
    }

    public void UpdateRecipe(Recipe recipe)
    {
        var dto = recipe.ToDto();
        var idx = _cache.FindIndex(r => r.Id == dto.Id);

        if (idx == -1) _cache.Add(dto);
        else _cache[idx] = dto;

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
        _cache = JsonSerializer.Deserialize<List<RecipeDto>>(json, _opts) ?? [];

        SaveFile();
    }

    private void SaveFile() =>
        File.WriteAllText(_filePath,
            JsonSerializer.Serialize(_cache, _opts));
}