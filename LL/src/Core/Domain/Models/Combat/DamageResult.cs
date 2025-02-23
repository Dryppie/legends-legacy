namespace Domain.Models.Combat;
public class DamageResult
{
    public int TotalDamage { get; set; }      // The total incoming damage (after flat reduction, but before the barrier)
    public int BarrierAbsorbed { get; set; }  // How much of that total is absorbed by the barrier
    public int HealthDamage { get; set; }     // How much makes it to HP (i.e., "true" damage to health)
    public bool IsCrit { get; set; }
}
