namespace Application.Interfaces.Services.LL;
public interface ISimulatorService
{
    Task SimulateCombat(int fights = 1, int tier = 1);
}