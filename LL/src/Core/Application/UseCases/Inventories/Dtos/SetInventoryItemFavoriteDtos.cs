namespace Application.UseCases.Inventories.Dtos;

public sealed record SetInventoryItemFavoriteRequestDto(bool IsFavorite);

public sealed class SetInventoryItemFavoriteResponseDto
{
    public Guid ItemInstanceId { get; set; }
    public bool IsFavorite { get; set; }
}
