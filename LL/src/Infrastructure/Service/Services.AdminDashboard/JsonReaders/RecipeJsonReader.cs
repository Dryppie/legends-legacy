using System.Text.Json;
using Application.UseCases._AdminDashboard.Items.Dtos;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting;

namespace Services.AdminDashboard.JsonReaders;
public class RecipeJsonReader
{
    public List<Recipe> AllRecipes { get; set; } = [];
    private string _filePath { get; set; }
    public RecipeJsonReader()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "recipes.json");
        string json = File.ReadAllText(_filePath);
        AllRecipes = JsonSerializer.Deserialize<List<Recipe>>(json) ?? [];
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