using System.Text.Json;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;

namespace Services.AdminDashboard.JsonReaders;
public class CreatureJsonReader
{
    public List<Creature> AllCreatures { get; set; } = [];
    private string _filePath { get; set; }

    public CreatureJsonReader()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "creatures.json");
        string json = File.ReadAllText(_filePath);

        AllCreatures = JsonSerializer.Deserialize<List<Creature>>(json)!;
        ValidateAndFixCreatureAttributes(AllCreatures);

        OverWriteJSON();
    }
    public List<Creature> GetCreaturesFromJson()
    {
        return AllCreatures;
    }

    public void UpdateCreatureFromCreature(CreatureDto creatureToUpdate)
    {

        var index = AllCreatures.FindIndex(c => c.Id == creatureToUpdate.Id);
        if (index != -1)
        {
            creatureToUpdate.UpdateProperties(AllCreatures[index]);
        }

        OverWriteJSON();
    }

    private void OverWriteJSON()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(AllCreatures, options));
    }

    private void ValidateAndFixCreatureAttributes(List<Creature> creatures)
    {
        var validAttributes = Enum.GetValues(typeof(AttributeType)).Cast<AttributeType>().ToList();

        foreach (var creature in creatures)
        {
            foreach (var attribute in validAttributes)
            {
                if (!creature.BaseAttributes.Any(ea => ea.AttributeType == attribute))
                {
                    creature.BaseAttributes.Add(new EntityAttribute
                    {
                        EntityId = creature.Id,
                        AttributeType = attribute,
                        Value = 0
                    });
                }

            }
        }
    }
}