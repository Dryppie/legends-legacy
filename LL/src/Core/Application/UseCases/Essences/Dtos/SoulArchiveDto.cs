namespace Application.UseCases.Essences.Dtos;

public sealed record SoulArchiveDto(IReadOnlyList<PlayerEssenceDto> Essences, int EssenceDust);
