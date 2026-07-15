namespace Application.Interfaces.Services.LL.Regions;

public interface IAreaExperienceBalanceProvider
{
    decimal GetTargetExperiencePerHour(string areaId);
    int CalculateEncounterExperience(string areaId, int creatureCount);
}
