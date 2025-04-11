namespace Domain.Models.Attributes;
public class EntityAttribute
{
    public Guid EntityId { get; set; }
    public AttributeType AttributeType { get; set; }
    public float Value { get; set; }
}