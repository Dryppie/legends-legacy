using Application.Interfaces.Services.LL;
using Common.Helpers;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Services.LL.Combat;

namespace Services.LL._Simulator;
public class SimulatorService : ISimulatorService
{
    private readonly ICombatService _combatService;
    private readonly IEntityService _entityService;
    private bool _pickRandomEssences = true;
    private string _essenceName;

    public SimulatorService(ICombatService combatService)
    {
        _combatService = combatService;
    }

    public async Task SimulateCombatWithOneEssence(string essenceName, int teamSize = 1)
    {
        _pickRandomEssences = false;
        _essenceName = essenceName;
        await SimulateCombat(teamSize, teamSize, 1, 1, 1);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerTeamSize">Size of player team</param>
    /// <param name="enemyTeamSize">Size of enemy team</param>
    /// <param name="fights">Amount of fights to simulate</param>
    /// <param name="tier">Number determining at what scale the fight is at. 1-10</param>
    /// <param name="locationId">Location is what region and area the monsters are from. Only added if simulating fight against monsters</param>
    /// <returns></returns>
    public async Task SimulateCombat(int playerTeamSize, int enemyTeamSize, int fights, int tier, int locationId)
    {
        var start = DateTimeOffset.Now;

        var numberOfDraws = 0;

        var essenceComboStats = new Dictionary<string, EssenceStat>(StringComparer.OrdinalIgnoreCase);
        var essences = EssenceLoader.Instance.GetEssences();

        // Initialize combatants
        var playerCharacters = GeneratePlayerTeam(playerTeamSize, tier);
        var enemyCharacters = GenerateEnemyTeam(enemyTeamSize, tier, locationId);

        var combatPlayerEntities = CreateCombatEntities(playerCharacters);
        var combatEnemyEntities = CreateCombatEntities(enemyCharacters);

        // Prepare entities for combat
        await PrepareEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

        var lastCombatResult = new CombatResult();

        while (fights > 0)
        {
            if (_pickRandomEssences)
                await PickRandomAbilities([.. combatPlayerEntities, .. combatEnemyEntities], tier);
            else
            {
                await PickSpecificAbility([.. combatPlayerEntities], _essenceName);
                await PickSpecificAbility([.. combatEnemyEntities]);
            }

            var combatSimulation = new CombatSimulation(combatPlayerEntities, combatEnemyEntities);
            lastCombatResult = combatSimulation.RunSimulation(simulated: true);

            // Build the combination keys for the essences
            var playerCombo = GetEssenceComboKey(combatPlayerEntities);
            var enemyCombo = GetEssenceComboKey(combatEnemyEntities);

            RecordMatchResult(_essenceStats, playerCombo, lastCombatResult.Outcome == BattleOutcome.Victory);
            RecordMatchResult(_essenceStats, enemyCombo, lastCombatResult.Outcome == BattleOutcome.Defeat);


            // Track draws
            if (lastCombatResult.Outcome.Equals(BattleOutcome.Draw))
            {
                numberOfDraws++;
            }

            ResetEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

            fights--;
        }

        // Sort results by win rate descending
        var resultList = _essenceStats.Values.OrderByDescending(a => a.WinRate).ToList();

        var end = DateTimeOffset.Now;

        // Print combination results
        foreach (var result in resultList)
        {
            Console.WriteLine($"{result.WinRate:0.##}% - {result.TimesWonWith}/{result.TimesUsed - result.TimesWonWith} - {result.TimesUsed} - {result.EssenceName}");
        }

        Console.WriteLine($"Draws : {numberOfDraws}");
        Console.WriteLine($"Time  : {end - start}");
    }

    private static List<Guid> SelectRandom(List<Guid> enemyTeam)
    {
        int randomCount = GetWeightedRandom();
        return enemyTeam.OrderBy(x => Guid.NewGuid()).Take(randomCount).ToList();
    }

    private static int GetWeightedRandom()
    {
        Random rand = new();
        int randomValue = rand.Next(0, 100);

        if (randomValue < 25) // 25% chance
            return 1;
        else if (randomValue < 80) // 55% chance
            return 2;
        else if (randomValue < 95) // 15% chance
            return 3;
        else // 5% chance
            return 4;
    }

    private static List<SimpleCombatEntity> CreateSimpleCombatEntities(List<Entity> playerCharacters)
    {
        var combatEntities = new List<SimpleCombatEntity>();

        foreach (var entity in playerCharacters)
        {
            combatEntities.Add(new SimpleCombatEntity(entity.Id.ToString(), entity.Name, (int)entity.BaseCombatAttributes[AttributeType.MaxHealth], (int)entity.BaseCombatAttributes[AttributeType.MaxMana], (int)entity.BaseCombatAttributes[AttributeType.Barrier]));
        }

        return combatEntities;
    }

    private static List<Entity> GeneratePlayerTeam(int teamSize, int tier)
    {
        return GenerateATeam(teamSize, tier, "Player");
    }

    private static List<Entity> GenerateEnemyTeam(int teamSize, int tier, int locationId)
    {
        //if (locationId > 0)
        //{
        //    return GetMonsters(locationId).Result;
        //}

        return GenerateATeam(teamSize, tier, "Enemy");
    }

    private static List<Entity> GenerateATeam(int teamSize, int tier, string teamName)
    {
        var team = new List<Entity>();
        for (int i = 0; i < teamSize; i++)
        {
            var entity = GenerateEntity(tier);
            entity.Id = Guid.NewGuid();
            entity.Name = $"{teamName}{i + 1}";
            team.Add(entity);
        }
        return team;
    }

    private static SimulatedEntity GenerateEntity(int tier)
    {
        var entity = new SimulatedEntity
        {
            BaseAttributes = CreateAttributes(tier)
        };

        return entity;
    }

    private static List<CombatEntity> CreateCombatEntities(List<Entity> entities)
    {
        var combatEntities = new List<CombatEntity>();

        foreach (var entity in entities)
        {
            combatEntities.Add(new CombatEntity(entity));
        }
        return combatEntities;
    }

    private static List<EntityAttribute> CreateAttributes(int tier)
    {
        return EntityBaseAttributeHelper.CreateSimulatedAttributes(tier);
    }

    //private async Task<List<Entity>> GetMonsters(int id)
    //{
    //    var ids = _entityService.GetEntityIdsByLocationId(id);
    //    return await _entityService.GetEntitiesByIdsForCombatAsync(ids);
    //}

    private static void ResetEntitiesForCombat(List<CombatEntity> allEntities)
    {
        foreach (var entity in allEntities)
        {
            entity.Reset();
            entity.EquippedEssences = [];
            entity.Abilities = [];
        }
    }

    private static async Task PrepareEntitiesForCombat(IEnumerable<CombatEntity> entities)
    {
        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(calculationTasks);
    }

    private static async Task PickRandomAbilities(IEnumerable<CombatEntity> entities, int tier)
    {
        // Load random abilities
        var attributePickerTasks = entities.Select(entity => Task.Run(() => EssenceLoader.Instance._Simulator_PickRandomAbilityCombinations(entity, tier)));

        await Task.WhenAll(attributePickerTasks);
    }

    private static async Task PickSpecificAbility(IEnumerable<CombatEntity> entities, string essenceName = "Test Essence")
    {
        // Pick ability for the first entity
        var tasks = new List<Task> { Task.Run(() => EssenceLoader.Instance._Simulator_PickSpecificAbility(entities.First(), essenceName)) };

        // Pick abilities for the remaining entities
        tasks.AddRange(entities.Skip(1).Select(entity => Task.Run(() => EssenceLoader.Instance._Simulator_PickRandomAbilityCombinations(entity))));

        await Task.WhenAll(tasks);
    }

    public class EssenceStat
    {
        public string EssenceName { get; set; } = string.Empty;
        public int TimesUsed { get; set; }
        public int TimesWonWith { get; set; }
        public double WinRate => TimesUsed == 0 ? 0.0 : Math.Round((double)TimesWonWith / TimesUsed * 100);
    }

    private static EssenceCombination GetEssenceComboKey(IEnumerable<CombatEntity> characters)
    {
        var essenceNames = characters
            .SelectMany(c => c.EquippedEssences.Select(e => e.Name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new EssenceCombination(essenceNames);
    }

    public record EssenceCombination(IReadOnlyList<string> EssenceNames)
    {
        public virtual bool Equals(EssenceCombination? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare lengths first
            if (EssenceNames.Count != other.EssenceNames.Count) return false;

            // Compare each element
            for (int i = 0; i < EssenceNames.Count; i++)
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(EssenceNames[i], other.EssenceNames[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Create a stable hash code based on the sequence
            var hash = new HashCode();
            foreach (var name in EssenceNames)
            {
                hash.Add(name, StringComparer.OrdinalIgnoreCase);
            }
            return hash.ToHashCode();
        }
    }
    private readonly Dictionary<EssenceCombination, EssenceStat> _essenceStats = [];

    private static void RecordMatchResult(Dictionary<EssenceCombination, EssenceStat> stats, EssenceCombination key, bool won)
    {
        if (!stats.TryGetValue(key, out var stat))
        {
            if (key is EssenceCombination combo)
            {
                stat = new EssenceStat { EssenceName = string.Join("+", combo.EssenceNames) };
            }
            else
            {
                // If it's a tuple key or another type, adjust accordingly.
                stat = new EssenceStat { EssenceName = key.ToString() ?? "Unknown" };
            }
            stats[key] = stat;
        }

        stat.TimesUsed++;
        if (won)
        {
            stat.TimesWonWith++;
        }
    }
}