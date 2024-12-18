namespace Domain.Models.Combat;
public class CalculatedResult
{
    public int CalculatedDamageToDeal { get; set; }
    public int CalculatedDamageReceived { get; set; }
    public AttackOutcome AttackOutcome { get; set; }
}