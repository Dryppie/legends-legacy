using Domain.Models.Damages;

namespace Domain.Models.Items.Equipments;
public class Weapon : Equipment
{
    public AttackType AttackType { get; set; }
    public DamageType DamageType { get; set; }
}