namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceCatalogDto(IReadOnlyList<EssenceDefinitionDto> Essences, IReadOnlyDictionary<string, IReadOnlyList<string>> TagsByCategory);
