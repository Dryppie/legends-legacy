namespace Application.UseCases.Characters.Dtos;

public sealed class WireCindersResponseDto
{
    public string RecipientName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public long RemainingCinders { get; set; }
}
