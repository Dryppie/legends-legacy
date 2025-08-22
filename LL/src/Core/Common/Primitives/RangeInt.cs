namespace Common.Primitives;
public sealed record RangeInt(int Min, int Max)
{
    public int Clamp(int value) => Math.Clamp(value, Min, Max);
    public override string ToString() => $"{Min}-{Max}";
}
