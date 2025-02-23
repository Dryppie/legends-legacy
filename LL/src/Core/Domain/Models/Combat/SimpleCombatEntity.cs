namespace Domain.Models.Combat;
public class SimpleCombatEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Barrier { get; set; }

    public SimpleCombatEntity(string id, string name, int maxHealth, int maxMana, int barrier)
    {
        Id = id;
        Name = name;
        Health = maxHealth;
        MaxHealth = maxHealth;
        Mana = maxMana;
        MaxMana = maxMana;
        Barrier = barrier;
    }

    public SimpleCombatEntity() { }
}