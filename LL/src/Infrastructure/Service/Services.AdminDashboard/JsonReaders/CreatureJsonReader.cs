using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;

namespace Services.AdminDashboard.JsonReaders;
public class CreatureJsonReader
{
    public List<Creature> GetCreaturesFromJson()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "creatures.json");
        string json = File.ReadAllText(filePath);

        var creatures = JsonSerializer.Deserialize<List<Creature>>(json);

        if (creatures == null || creatures.Count == 0)
        {
            return [];
        }

        ValidateAndFixCreatureAttributes(creatures);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(creatures, options));

        return creatures;
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