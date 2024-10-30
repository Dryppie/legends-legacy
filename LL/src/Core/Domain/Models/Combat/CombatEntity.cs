namespace Domain.Models.Combat;
public class CombatEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }

    public CombatEntity(Guid id, string name, int maxHealth, int maxMana)
    {
        Id = id;
        Name = name;
        Health = maxHealth;
        MaxHealth = maxHealth;
        Mana = maxMana;
        MaxMana = maxMana;
    }
}