using Domain.Models.Attributes;

namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceAttributeBonusDto(AttributeType Attribute, string ModifierKind, double BaseValue, double PerLevel, double PerAscensionTier, double CurrentValue);
