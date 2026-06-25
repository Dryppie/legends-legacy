namespace Domain.Models.Prophecies;

public sealed class ProphecyProgressSnapshot
{
    public List<string> UniqueIds { get; set; } = [];
    public bool HasMeaningfulDefeat { get; set; }
}
