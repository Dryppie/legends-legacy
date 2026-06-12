namespace Domain.Models.Essences.Definitions;

public static class EssenceTagCatalog
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> TagsByCategory =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Species"] =
            [
                "Species.Beast", "Species.Insectoid", "Species.Undead", "Species.Demon", "Species.Humanoid",
                "Species.Goblinoid", "Species.Orc", "Species.Troll", "Species.Construct", "Species.Elemental",
                "Species.Plant", "Species.Dragonkin", "Species.Spirit", "Species.Aberration", "Species.Aquatic",
                "Species.Reptile", "Species.Avian", "Species.Giant", "Species.Slime", "Species.Vermin"
            ],
            ["Role"] =
            [
                "Role.Assassin", "Role.Tank", "Role.Defensive", "Role.Offensive", "Role.Support",
                "Role.Controller", "Role.Summoner", "Role.Caster", "Role.Debuffer"
            ],
            ["Range"] = ["Range.Melee", "Range.Ranged"],
            ["Element"] = ["Element.Physical", "Element.Fire", "Element.Shadow", "Element.Necrotic"],
            ["Pattern"] = ["Pattern.SingleTarget", "Pattern.Area", "Pattern.DamageOverTime", "Pattern.Periodic"],
            ["Defense"] = ["Defense.Block", "Defense.Guard", "Defense.Barrier", "Defense.Dodge"],
            ["Effect"] =
            [
                "Effect.None", "Effect.BasicAttack", "Effect.Ability", "Effect.Melee", "Effect.Ranged",
                "Effect.Physical", "Effect.Magical", "Effect.Poison", "Effect.Burn", "Effect.Bleed",
                "Effect.Holy", "Effect.Healing", "Effect.Summon", "Effect.Barrier", "Effect.CrowdControl"
            ],
            ["Control"] = ["Control.Stun", "Control.Freeze", "Control.Fear", "Control.Taunt", "Control.Blind", "Control.Interrupt", "Control.Suppression"],
            ["Status"] = ["Status.Bleed", "Status.Burn", "Status.Poison", "Status.Chill", "Status.Shock", "Status.Vulnerable", "Status.Weakened", "Status.Curse"],
            ["Resource"] = ["Resource.Health", "Resource.Barrier", "Resource.Cooldown"],
            ["Trigger"] =
            [
                "Trigger.OnCombatStart", "Trigger.OnCombatEnd", "Trigger.OnHit", "Trigger.OnCrit", "Trigger.OnKill",
                "Trigger.OnTakeDamage", "Trigger.OnDodge", "Trigger.OnBlock", "Trigger.OnBarrierBreak",
                "Trigger.OnLowHealth", "Trigger.OnAllyLowHealth", "Trigger.OnInterval", "Trigger.OnStatusApplied",
                "Trigger.OnStatusExpired", "Trigger.OnSummonDeath", "Trigger.OnAbilityUse", "Trigger.OnBasicAttack"
            ],
            ["Target"] =
            [
                "Target.Self", "Target.Ally", "Target.LowestHealthAlly", "Target.Enemy", "Target.RandomEnemy",
                "Target.LowestHealthEnemy", "Target.HighestThreatEnemy", "Target.Frontline", "Target.Backline",
                "Target.Area", "Target.Adjacent", "Target.SummonOwner", "Target.Summon"
            ],
            ["Mechanic"] = ["Mechanic.Execute"]
        };

    public static readonly IReadOnlySet<string> AllTags = TagsByCategory
        .SelectMany(kv => kv.Value)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static string GetCategory(string tag)
    {
        var separator = tag.IndexOf('.');
        return separator <= 0 ? "Unknown" : tag[..separator];
    }
}
