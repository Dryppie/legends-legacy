namespace Application.UseCases.Essences.Dtos;

public sealed record SpendEssenceDustResultDto(bool Succeeded, string Message, int DustSpent, int XpGained, int LevelsGained, bool ReachedTierCap);
