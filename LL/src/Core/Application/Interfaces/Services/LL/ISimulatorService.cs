namespace Application.Interfaces.Services.LL;
public interface ISimulatorService
{
    Task SimulateCombat(int playerTeamSize, int enemyTeamSize, int fights, int tier, int locationId);
    Task SimulateCombatWithOneEssence(string essenceName);
}