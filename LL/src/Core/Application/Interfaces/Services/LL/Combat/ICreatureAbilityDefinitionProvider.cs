namespace Application.Interfaces.Services.LL.Combat;

public interface ICreatureAbilityDefinitionProvider
{
    IReadOnlyList<string> GetAbilityIds(string monsterDefinitionId);
}
