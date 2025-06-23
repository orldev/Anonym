namespace Client.Core.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DateTime"/> comparisons.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Determines if two DateTime values are equal within a specified tolerance.
    /// </summary>
    /// <param name="first">The first DateTime to compare.</param>
    /// <param name="second">The second DateTime to compare.</param>
    /// <param name="tolerance">The maximum allowed difference between the dates.</param>
    /// <returns>
    /// true if the absolute difference between the dates is less than or equal to the tolerance;
    /// otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method handles comparison in both directions (first before second or second before first)
    /// by using the absolute value of the time difference.
    /// Example use cases:
    /// - Comparing timestamps where minor differences are acceptable
    /// - Testing scenarios requiring fuzzy time matching
    /// - Validating time-sensitive operations with allowable drift
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime date1 = DateTime.Now;
    /// DateTime date2 = date1.AddMilliseconds(50);
    /// bool areEqual = date1.EqualsWithin(date2, TimeSpan.FromMilliseconds(100)); // returns true
    /// </code>
    /// </example>
    public static bool EqualsWithin(this DateTime first, DateTime second, TimeSpan tolerance)
    {
        return (first - second).Duration() <= tolerance;
    }
}