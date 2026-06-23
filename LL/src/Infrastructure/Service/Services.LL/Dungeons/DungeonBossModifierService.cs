using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonBossModifierService : IDungeonBossModifierService
{
    public IReadOnlyList<DungeonBossModifier> GetActiveBossModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(room);

        run.State ??= new DungeonRunState { RunId = run.Id };
        if (room.Type != RoomType.Boss)
        {
            return [];
        }

        var modifiers = new List<DungeonBossModifier>();
        AddPressureThresholdModifiers(modifiers, run, dungeon.Mechanic);
        AddFlagModifiers(modifiers, run, dungeon);

        return modifiers
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    public IReadOnlyList<AttributeModifierBase> GetActiveBossAttributeModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room) =>
        GetActiveBossModifiers(run, dungeon, room)
            .Select(x => new DungeonAttributeModifier(x.AttributeType, x.Amount, x.ModifierType))
            .Cast<AttributeModifierBase>()
            .ToList();

    private static void AddPressureThresholdModifiers(
        List<DungeonBossModifier> modifiers,
        DungeonRun run,
        DungeonMechanicDefinition mechanic)
    {
        var thresholdModifiers = mechanic.Thresholds.Count > 0
            ? mechanic.Thresholds
                .Where(x => run.State.Pressure >= x.Value)
                .SelectMany(x => x.BossModifierIds)
                .ToList()
            : GetDefaultPressureModifierIds(run.State.Pressure).ToList();

        foreach (var modifierId in thresholdModifiers)
        {
            AddKnownModifier(modifiers, modifierId, "Mechanic");
        }
    }

    private static IEnumerable<string> GetDefaultPressureModifierIds(int pressure)
    {
        if (pressure >= 75)
        {
            yield return "boss_pressure_enraged";
        }
        else if (pressure >= 50)
        {
            yield return "boss_pressure_alert";
        }
        else if (pressure >= 25)
        {
            yield return "boss_pressure_ready";
        }
    }

    private static void AddFlagModifiers(
        List<DungeonBossModifier> modifiers,
        DungeonRun run,
        DungeonDefinition dungeon)
    {
        var flags = run.State.Flags;
        var checkpointPushes = flags.GetValueOrDefault("checkpoint_pushes");
        if (checkpointPushes > 0)
        {
            modifiers.Add(new DungeonBossModifier
            {
                Id = "checkpoint_push_boss_fury",
                Name = "Pushed Too Deep",
                Description = checkpointPushes == 1
                    ? "The boss has had time to gather itself."
                    : "Repeated pushes have let the boss gather itself.",
                Source = "Checkpoint",
                AttributeType = AttributeType.Power,
                Amount = Math.Min(15, checkpointPushes * 5),
                ModifierType = ModifierType.Additive
            });
        }

        AddFlagModifier(modifiers, flags, "searched_deep_treasure", "boss_treasure_guard", "Guarded Hoard", "The boss fights harder after the hoard was disturbed.", "Event", AttributeType.Power, 5, ModifierType.Additive);
        AddFlagModifier(modifiers, flags, "boss_reinforcements_reduced", "boss_reinforcements_reduced", "Reduced Reinforcements", "Collapsed routes leave the boss with less support.", "Event", AttributeType.MaxHealth, -10, ModifierType.Additive, true);
        AddFlagModifier(modifiers, flags, "cleansed_shrine", "boss_corruption_weakened", "Corruption Cleansed", "Cleansed shrine power weakens the final foe.", "Event", AttributeType.Resistance, -8, ModifierType.Additive, true);
        AddFlagModifier(modifiers, flags, "hidden_route_taken", "boss_surprised", "Ambushed From Within", "Taking a hidden route catches the boss off guard.", "Route", AttributeType.BlockChance, -5, ModifierType.Flat, true);

        if (dungeon.Id.StartsWith("goblin_mines", StringComparison.OrdinalIgnoreCase))
        {
            AddFlagModifier(modifiers, flags, "saved_explosives", "goblin_saved_explosives", "Saved Explosives", "Stored blasting powder weakens the hobgoblin.", "Goblin Mines", AttributeType.Armor, -12, ModifierType.Additive, true);
            AddFlagModifier(modifiers, flags, "collapsed_tunnel", "goblin_tunnel_collapsed", "Collapsed Tunnel", "The hobgoblin loses tunnel reinforcements.", "Goblin Mines", AttributeType.MaxHealth, -10, ModifierType.Additive, true);
            AddFlagModifier(modifiers, flags, "saved_miner", "goblin_miner_aid", "Miner's Aid", "The rescued miner's guidance exposes the boss's weak points.", "Goblin Mines", AttributeType.DodgeChance, -5, ModifierType.Flat, true);
            AddFlagModifier(modifiers, flags, "goblin_powder_looted", "goblin_powder_alarm", "Powder Alarm", "Looting the powder cache leaves the hobgoblin ready for trouble.", "Goblin Mines", AttributeType.Precision, 5, ModifierType.Additive);
        }

        if (dungeon.Id.StartsWith("forgotten_catacombs", StringComparison.OrdinalIgnoreCase))
        {
            AddFlagModifier(modifiers, flags, "cleansed_tomb", "catacombs_cleansed_tomb", "Cleansed Tomb", "A cleansed tomb loosens the wraith's hold.", "Forgotten Catacombs", AttributeType.Spirit, -8, ModifierType.Additive, true);
            AddFlagModifier(modifiers, flags, "sealed_reliquary", "catacombs_sealed_reliquary", "Sealed Reliquary", "Sealing the reliquary denies the wraith a relic.", "Forgotten Catacombs", AttributeType.Power, -10, ModifierType.Additive, true);
            AddFlagModifier(modifiers, flags, "opened_reliquary", "catacombs_opened_reliquary", "Opened Reliquary", "Opening the reliquary feeds the wraith's curse.", "Forgotten Catacombs", AttributeType.Spirit, 10, ModifierType.Additive);
            AddFlagModifier(modifiers, flags, "bound_spirit_power", "catacombs_bound_spirit", "Bound Spirit Power", "Bound spirit power echoes into the final shade.", "Forgotten Catacombs", AttributeType.Resistance, 6, ModifierType.Additive);
        }
    }

    private static void AddFlagModifier(
        List<DungeonBossModifier> modifiers,
        Dictionary<string, int> flags,
        string flag,
        string id,
        string name,
        string description,
        string source,
        AttributeType attributeType,
        float amount,
        ModifierType modifierType,
        bool isHelpfulToPlayer = false)
    {
        if (flags.GetValueOrDefault(flag) <= 0)
        {
            return;
        }

        modifiers.Add(new DungeonBossModifier
        {
            Id = id,
            Name = name,
            Description = description,
            Source = source,
            AttributeType = attributeType,
            Amount = amount,
            ModifierType = modifierType,
            IsHelpfulToPlayer = isHelpfulToPlayer
        });
    }

    private static void AddKnownModifier(
        List<DungeonBossModifier> modifiers,
        string modifierId,
        string source)
    {
        var modifier = modifierId switch
        {
            "boss_pressure_ready" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Battle Ready",
                Description = "Rising pressure has the boss prepared.",
                Source = source,
                AttributeType = AttributeType.Armor,
                Amount = 5,
                ModifierType = ModifierType.Additive
            },
            "boss_pressure_alert" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "On Alert",
                Description = "High pressure sharpens the boss's attacks.",
                Source = source,
                AttributeType = AttributeType.Power,
                Amount = 8,
                ModifierType = ModifierType.Additive
            },
            "boss_pressure_enraged" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Enraged",
                Description = "Extreme pressure empowers the boss.",
                Source = source,
                AttributeType = AttributeType.Power,
                Amount = 15,
                ModifierType = ModifierType.Additive
            },
            "alarm_boss_ready" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Alarmed Guard",
                Description = "The alarm lets the boss brace for impact.",
                Source = source,
                AttributeType = AttributeType.Armor,
                Amount = 6,
                ModifierType = ModifierType.Additive
            },
            "alarm_boss_enraged" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Full Alarm",
                Description = "The boss is fully alerted and strikes harder.",
                Source = source,
                AttributeType = AttributeType.Power,
                Amount = 12,
                ModifierType = ModifierType.Additive
            },
            "curse_boss_shrouded" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Curse Shroud",
                Description = "The curse shields the boss from harm.",
                Source = source,
                AttributeType = AttributeType.Resistance,
                Amount = 8,
                ModifierType = ModifierType.Additive
            },
            "curse_boss_empowered" => new DungeonBossModifier
            {
                Id = modifierId,
                Name = "Curse Empowered",
                Description = "The curse feeds the boss's power.",
                Source = source,
                AttributeType = AttributeType.Spirit,
                Amount = 12,
                ModifierType = ModifierType.Additive
            },
            _ => null
        };

        if (modifier is not null)
        {
            modifiers.Add(modifier);
        }
    }
}
