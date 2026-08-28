namespace LegendsLegacy.Balance;

public enum ProgressionCurveKind
{
    Linear,
    EaseIn,
    EaseOut,
    SmoothStep
}

public sealed record ProgressionBandOptions(ProgressionCurveKind Curve = ProgressionCurveKind.SmoothStep)
{
    public ProgressionBandOptions Validate()
    {
        if (!Enum.IsDefined(Curve))
            throw new ArgumentOutOfRangeException(nameof(Curve), Curve, "Unsupported progression curve.");
        return this;
    }
}

public sealed record ProgressionBandDefinition(
    string Id,
    int StartFloor,
    int EndFloor,
    string StartAnchorId,
    string EndAnchorId);

public sealed record ProgressionFloorTargetSnapshot(
    int Floor,
    double NormalizedPosition,
    double CurveWeight,
    double TargetBenchmarkPower,
    string? AnchorId);

public sealed record ProgressionBandSnapshot(
    ProgressionBandDefinition Definition,
    ProgressionCurveKind Curve,
    double StartBenchmarkPower,
    double EndBenchmarkPower,
    IReadOnlyList<ProgressionFloorTargetSnapshot> Floors);

public sealed record ProgressionBandSuiteSnapshot(
    int AlgorithmVersion,
    ProgressionBandOptions Options,
    IReadOnlyList<ProgressionBandSnapshot> Bands);

public sealed class ProgressionBandBuilder
{
    public const int AlgorithmVersion = 1;

    public static ProgressionBandDefinition RegionOneDefinition { get; } = new(
        "WorldTower.Region1",
        1,
        10,
        "WorldTower.Region1.Start",
        "WorldTower.Region1.End");

    public ProgressionBandSuiteSnapshot Create(
        PowerAnchorSuiteSnapshot powerAnchors,
        ProgressionBandOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(powerAnchors);
        var options = (requestedOptions ?? new ProgressionBandOptions()).Validate();
        var definition = RegionOneDefinition;
        var start = ResolveAnchor(powerAnchors, definition.StartAnchorId, definition.StartFloor);
        var end = ResolveAnchor(powerAnchors, definition.EndAnchorId, definition.EndFloor);
        var startPower = start.Performance.MeanBenchmarkPower;
        var endPower = end.Performance.MeanBenchmarkPower;
        var floorSpan = definition.EndFloor - definition.StartFloor;
        if (floorSpan <= 0)
            throw new InvalidOperationException($"Progression band '{definition.Id}' has an invalid floor range.");

        var floors = Enumerable.Range(definition.StartFloor, floorSpan + 1)
            .Select(floor =>
            {
                var position = (floor - definition.StartFloor) / (double)floorSpan;
                var weight = ApplyCurve(position, options.Curve);
                var target = floor switch
                {
                    var value when value == definition.StartFloor => startPower,
                    var value when value == definition.EndFloor => endPower,
                    _ => startPower + (endPower - startPower) * weight
                };
                var anchorId = floor switch
                {
                    var value when value == definition.StartFloor => definition.StartAnchorId,
                    var value when value == definition.EndFloor => definition.EndAnchorId,
                    _ => null
                };
                return new ProgressionFloorTargetSnapshot(
                    floor,
                    Round(position, 4),
                    Round(weight, 6),
                    Round(target, 2),
                    anchorId);
            })
            .ToArray();

        return new ProgressionBandSuiteSnapshot(
            AlgorithmVersion,
            options,
            [new ProgressionBandSnapshot(definition, options.Curve, startPower, endPower, floors)]);
    }

    internal static double ApplyCurve(double position, ProgressionCurveKind curve)
    {
        if (position is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(position), "Curve position must be between 0 and 1.");
        return curve switch
        {
            ProgressionCurveKind.Linear => position,
            ProgressionCurveKind.EaseIn => position * position,
            ProgressionCurveKind.EaseOut => 1 - Math.Pow(1 - position, 2),
            ProgressionCurveKind.SmoothStep => position * position * (3 - 2 * position),
            _ => throw new ArgumentOutOfRangeException(nameof(curve), curve, "Unsupported progression curve.")
        };
    }

    private static PowerAnchorSnapshot ResolveAnchor(
        PowerAnchorSuiteSnapshot suite,
        string anchorId,
        int expectedFloor)
    {
        var matches = suite.Anchors.Where(anchor => anchor.Definition.Id == anchorId).Take(2).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                matches.Length == 0
                    ? $"Power anchor '{anchorId}' was not found for progression-band construction."
                    : $"Power anchor '{anchorId}' is duplicated for progression-band construction.");
        }
        if (matches[0].Definition.Floor != expectedFloor)
        {
            throw new InvalidOperationException(
                $"Power anchor '{anchorId}' targets Floor {matches[0].Definition.Floor}, expected Floor {expectedFloor}.");
        }
        return matches[0];
    }

    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
