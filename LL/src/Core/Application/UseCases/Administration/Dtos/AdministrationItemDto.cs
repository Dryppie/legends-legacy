using Domain.Models.Items;

namespace Application.UseCases.Administration.Dtos;

public sealed record AdministrationItemDto(
    string Id,
    string Name,
    string Description,
    ItemType ItemType,
    Rarity Rarity,
    bool Stackable,
    bool IsBound);
