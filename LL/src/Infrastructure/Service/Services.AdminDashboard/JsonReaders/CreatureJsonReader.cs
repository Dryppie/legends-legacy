using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Common.Utilities.EnumConverters;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;

namespace Services.AdminDashboard.JsonReaders;
public class CreatureJsonReader
{
    private static readonly object FileLock = new();
    public List<Creature> AllCreatures { get; set; } = [];
    private readonly string _filePath;

    public CreatureJsonReader()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Directory.GetParent(currentDirectory)!.FullName;
        _filePath = Path.Combine(apiDirectory, "API.LL", "Data", "creatures.json");
        string json;
        lock (FileLock)
        {
            json = ReadAllTextShared(_filePath);
        }

        AllCreatures = JsonSerializer.Deserialize<List<Creature>>(json, new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new SafeEnumConverter<AttributeType>(), new JsonStringEnumConverter() }
        })!;

        if (ValidateAndFixCreatureAttributes(AllCreatures))
        {
            OverWriteJSON();
        }
    }
    public List<Creature> GetCreaturesFromJson()
    {
        return AllCreatures;
    }

    public void UpdateCreatureFromCreature(CreatureDto creatureToUpdate)
    {
        lock (FileLock)
        {
            var index = AllCreatures.FindIndex(c => c.Id == creatureToUpdate.Id);
            if (index != -1)
            {
                creatureToUpdate.UpdateProperties(AllCreatures[index]);
            }

            OverWriteJSONUnsafe();
        }
    }

    private void OverWriteJSON()
    {
        lock (FileLock)
        {
            OverWriteJSONUnsafe();
        }
    }

    private void OverWriteJSONUnsafe()
    {
        var options = new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        var json = JsonSerializer.Serialize(AllCreatures, options);
        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static string ReadAllTextShared(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private bool ValidateAndFixCreatureAttributes(List<Creature> creatures)
    {
        var changed = false;
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
                    changed = true;
                }

            }
        }

        return changed;
    }
}