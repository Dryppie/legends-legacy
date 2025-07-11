namespace Domain.Models.Combat;
public sealed class AbilityStats
{
    public string Name { get; set; } = string.Empty;
    public int TotalDamage { get; set; }
    public int TotalHealing { get; set; }
    public int Hits { get; set; }
    public int Crits { get; set; }

    public double AvgDamage => Hits == 0 ? 0 : (double)TotalDamage / Hits;
    public double AvgHealing => Hits == 0 ? 0 : (double)TotalHealing / Hits;

    public void Apply(CombatLogEntry e)
    {
        if (e.EventType == EventType.Damage || e.EventType == EventType.DamageOverTime)
            TotalDamage += e.Amount;
        else if (e.EventType == EventType.Heal)
            TotalHealing += e.Amount;

        if (e.EventType is EventType.Damage or EventType.Heal)
        {
            Hits++;
            if (e.IsCrit) Crits++;
        }
    }
}