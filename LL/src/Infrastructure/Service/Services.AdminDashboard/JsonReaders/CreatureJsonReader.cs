using System.Text.Json;
using System.Text.Json.Serialization;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Common.Utilities.EnumConverters;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Services.AdminDashboard.JsonReaders;

public class CreatureJsonReader
{
    private static readonly object FileLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new SafeEnumConverter<AttributeType>(), new JsonStringEnumConverter() }
    };

    public List<Creature> AllCreatures { get; set; } = [];
    private CreatureSeedCatalog _catalog = new();
    private readonly string _filePath;

    public CreatureJsonReader()
    {
        _filePath = FindDataFile(Path.Combine("world", "creatures.json"));
        string json;
        lock (FileLock)
        {
            json = ReadAllTextShared(_filePath);
        }

        _catalog = JsonSerializer.Deserialize<CreatureSeedCatalog>(json, JsonOptions)!;
        AllCreatures = _catalog.Creatures.Select(ToCreature).ToList();
        ValidateAndFixCreatureAttributes(AllCreatures);
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

            var seed = _catalog.Creatures.FirstOrDefault(creature => creature.Id == creatureToUpdate.Id);
            if (seed is not null)
            {
                seed.Name = creatureToUpdate.Name;
                seed.ExperienceReward = creatureToUpdate.ExperienceReward;
                seed.BaseLevel = creatureToUpdate.Level;
            }

            OverWriteJSONUnsafe();
        }
    }

    private void OverWriteJSONUnsafe()
    {
        var json = JsonSerializer.Serialize(_catalog, JsonOptions);
        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static Creature ToCreature(CreatureSeed seed)
    {
        var creature = new Creature
        {
            Id = seed.Id,
            Name = seed.Name,
            ImagePath = seed.ImagePath,
            ExperienceReward = seed.ExperienceReward,
            Archetype = seed.Archetype,
            DamageProfile = seed.DamageProfile,
            DefenseProfile = seed.DefenseProfile,
            RewardTableId = seed.RewardTableId,
            BaseLevel = seed.BaseLevel,
            Level = seed.BaseLevel,
            Tier = seed.Tier,
            StatOverrides = seed.StatOverrides
                .Select(statOverride => new StatOverride
                {
                    Id = statOverride.Id,
                    AttributeType = statOverride.AttributeType,
                    Multiplier = statOverride.Multiplier,
                    Additive = statOverride.Additive
                })
                .ToList()
        };

        creature.BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(creature.Id);
        return creature;
    }

    private static string ReadAllTextShared(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string FindDataFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "Data", fileName),
                Path.Combine(directory.FullName, "src", "API", "API.LL", "Data", fileName),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data", fileName)
            })
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate Data/{fileName} from '{AppContext.BaseDirectory}'.");
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

    private sealed class CreatureSeedCatalog
    {
        public List<CreatureSeed> Creatures { get; set; } = [];
    }

    private sealed class CreatureSeed
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int ExperienceReward { get; set; }
        public CreatureArchetype Archetype { get; set; } = CreatureArchetype.Balanced;
        public DamageProfile DamageProfile { get; set; } = DamageProfile.Hybrid;
        public DefenseProfile DefenseProfile { get; set; } = DefenseProfile.Balanced;
        public string? RewardTableId { get; set; }
        public int BaseLevel { get; set; } = 1;
        public int Tier { get; set; } = 1;
        public List<CreatureStatOverrideSeed> StatOverrides { get; set; } = [];
    }

    private sealed class CreatureStatOverrideSeed
    {
        public Guid Id { get; set; }
        public AttributeType AttributeType { get; set; }
        public float? Multiplier { get; set; }
        public float? Additive { get; set; }
    }
}
