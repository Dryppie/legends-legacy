using System.Text.Json;
using Application.UseCases._AdminDashboard.Items.Dtos;
using Domain.Models.Items;

namespace Services.AdminDashboard.JsonReaders;
public class ItemBaseJsonReader
{
    public List<ItemBase> AllItems { get; set; } = [];
    private string _filePath { get; set; }
    public ItemBaseJsonReader()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "items.json");
        string json = File.ReadAllText(_filePath);
        AllItems = JsonSerializer.Deserialize<List<ItemBase>>(json) ?? [];
        OverWriteJSON();
    }
    public List<ItemBase> GetItemsFromJson()
    {
        return AllItems;
    }
    public void UpdateItemFromItemBase(ItemBaseDto itemToUpdate)
    {
        var index = AllItems.FindIndex(c => c.Id == itemToUpdate.Id);
        if (index == -1)
            AllItems.Add(new ItemBase()
            {
                Id = itemToUpdate.Id,
                Name = itemToUpdate.Name,
                Description = itemToUpdate.Description,
                ItemType = itemToUpdate.ItemType,
                Rarity = itemToUpdate.Rarity
            });
        else
            itemToUpdate.UpdateProperties(AllItems[index]);

        OverWriteJSON();
    }
    private void OverWriteJSON()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(AllItems, options));
    }

    public void AddItemBase(ItemBase itemToAdd)
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