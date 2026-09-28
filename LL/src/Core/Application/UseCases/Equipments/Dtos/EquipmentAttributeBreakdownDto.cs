using Domain.Models.Attributes;

namespace Application.UseCases.Equipments.Dtos;

public sealed record EquipmentAttributeBreakdownDto(AttributeType AttributeType, float Base, float WithEquipment,
    float WithSetsAndLoadout, float Effective, float UnusedAtCap);
