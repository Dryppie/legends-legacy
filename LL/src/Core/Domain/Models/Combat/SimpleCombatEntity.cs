namespace Domain.Models.Combat;
public class SimpleCombatEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImagePath {  get; set; } = string.Empty;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Barrier { get; set; }
    public float Threat { get; set; }
    public int Level { get; set; } = 1;
    public int? PartyNumber { get; set; }
    public int CurrentStagger { get; set; }
    public int MaxStagger { get; set; }
    public bool IsStaggered { get; set; }
    public bool IsStaggerRecovering { get; set; }

    public SimpleCombatEntity(string id, string name, string imagePath, int maxHealth, int barrier, int level = 1)
    {
        Id = id;
        Name = name;
        ImagePath = imagePath;
        Health = maxHealth;
        MaxHealth = maxHealth;
        Barrier = barrier;
        Level = Math.Max(1, level);
    }

    public SimpleCombatEntity() { }
}
