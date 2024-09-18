namespace Common.Extensions;
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Calculates how many times X seconds can pass between two DateTimeOffset instances.
    /// Rounds down to nearest integer.
    /// </summary>
    /// <param name="start">The start DateTimeOffset.</param>
    /// <param name="end">The end DateTimeOffset.</param>
    /// <param name="seconds">The number of seconds for each interval.</param>
    /// <returns>The number of times X seconds can pass between the two times.</returns>
    public static int NumberOfXSecondsIntervals(this DateTimeOffset start, DateTimeOffset end, int seconds)
    {
        var timeSpan = end - start;
        return (int)(timeSpan.TotalSeconds / seconds);
    }
}