namespace Application.UseCases.Essences.Dtos;

public sealed record DismantleEssenceResultDto(bool Succeeded, string Message, int DustGained);
