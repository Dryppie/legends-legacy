namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceEvolutionDto(string Id, string Name, string Description, int RequiredAscensionTier, string RequiredCatalystItemId, IReadOnlyList<string> AddsTags);
