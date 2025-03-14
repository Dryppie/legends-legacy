namespace Domain.Interfaces.Leveling;
public interface ILevelAction
{
    Task Execute(Guid characterId);
}
