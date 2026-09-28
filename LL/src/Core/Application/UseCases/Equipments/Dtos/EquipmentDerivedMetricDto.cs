namespace Application.UseCases.Equipments.Dtos;

public sealed record EquipmentDerivedMetricDto(string Id, string Label, double Before, double After, string Unit, string Assumption);
