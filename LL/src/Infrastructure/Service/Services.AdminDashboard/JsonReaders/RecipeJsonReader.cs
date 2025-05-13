using System.Text.Json;
using Domain.Models.Professions.Crafting;

namespace Services.AdminDashboard.JsonReaders;
public class RecipeJsonReader
{
    public List<Recipe> AllRecipes { get; set; } = [];
    private readonly string _filePath;
    public RecipeJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;
        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "recipes.json");
        string json = File.ReadAllText(_filePath);

        AllRecipes = JsonSerializer.Deserialize<List<Recipe>>(json) ?? [];
        foreach (var recipe in AllRecipes)
        {
            foreach (var material in recipe.Materials)
            {
                material.ItemId = material.Item.Id;
            }
        }
        OverWriteJSON();
    }
    public List<Recipe> GetRecipesFromJson()
    {
        return AllRecipes;
    }
    public void UpdateRecipe(Recipe recipeToUpdate)
    {
        var index = AllRecipes.FindIndex(c => c.Id == recipeToUpdate.Id);
        if (index == -1)
            AllRecipes.Add(recipeToUpdate);
        else
            AllRecipes[index] = recipeToUpdate;

        OverWriteJSON();
    }

    private void OverWriteJSON()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(AllRecipes, options));
    }

    public void AddItemBase(Recipe recipeToAdd)
    {
        AllRecipes.Add(recipeToAdd);
        OverWriteJSON();
    }

    public void RemoveItemBaseById(string id)
    {
        var index = AllRecipes.FindIndex(c => c.Id.ToString() == id);
        if (index != -1)
        {
            AllRecipes.RemoveAt(index);
        }
        OverWriteJSON();
    }
}