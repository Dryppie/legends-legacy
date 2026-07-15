namespace Application.Interfaces.Services.LL.Regions;

public interface IAreaExperienceBalanceProvider
{
    decimal GetTargetExperiencePerHour(string areaId);
    decimal GetTargetCindersPerHour(string areaId);
    int CalculateEncounterExperience(string areaId, int creatureCount);
    int CalculateEncounterCinders(string areaId, int creatureCount);
}
