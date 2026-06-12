namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceEffectDto(string Id, string Type, string Target, double CurrentValue, string? Attribute, string? Status, double? DurationSeconds);
