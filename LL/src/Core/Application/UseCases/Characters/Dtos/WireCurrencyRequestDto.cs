namespace Application.UseCases.Characters.Dtos;

public sealed class WireCurrencyRequestDto
{
    public string RecipientName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
