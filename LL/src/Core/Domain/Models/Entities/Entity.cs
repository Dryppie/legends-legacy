namespace Domain.Models.Entities;
public abstract class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    //public ICollection<Modifier> Modifiers { get; set; } = [];
}