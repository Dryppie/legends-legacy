using System.Text.Json;
using Application.UseCases._AdminDashboard.Items.Dtos;

namespace Services.AdminDashboard.JsonReaders;
public class ItemBaseJsonReader
{
    public List<ItemBaseDto> AllItems { get; set; } = [];
    private readonly string _filePath;
    public ItemBaseJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;
        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "items.json");
        string json = File.ReadAllText(_filePath);

        AllItems = JsonSerializer.Deserialize<List<ItemBaseDto>>(json) ?? [];
        OverWriteJSON();
    }
    public List<ItemBaseDto> GetItemsFromJson()
    {
        return AllItems;
    }
    public void UpdateItemFromItemBase(ItemBaseDto itemToUpdate)
    {
        var index = AllItems.FindIndex(c => c.Id == itemToUpdate.Id);
        if (index == -1)
            AllItems.Add(itemToUpdate);
        else
            AllItems[index] = itemToUpdate;

        OverWriteJSON();
    }

    private void OverWriteJSON()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(AllItems, options));
    }

    public void AddItemBase(ItemBaseDto itemToAdd)
    {
        AllItems.Add(itemToAdd);
        OverWriteJSON();
    }

    public void RemoveItemBaseById(string id)
    {
        var index = AllItems.FindIndex(c => c.Id == id);
        if (index != -1)
        {
            AllItems.RemoveAt(index);
        }
        OverWriteJSON();
    }
}