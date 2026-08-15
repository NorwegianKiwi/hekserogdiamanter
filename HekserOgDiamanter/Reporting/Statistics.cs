namespace HekserOgDiamanter.Reporting;

public sealed record DistributionStatistics(double Mean, int P95, int P99, int P999, int Maximum);

public static class Statistics
{
    public static DistributionStatistics Calculate(IEnumerable<int> source)
    {
        var values = source.Order().ToArray();
        if (values.Length == 0) throw new InvalidOperationException("Cannot calculate statistics for an empty sequence.");
        return new DistributionStatistics(
            values.Average(),
            NearestRank(values, 0.95),
            NearestRank(values, 0.99),
            NearestRank(values, 0.999),
            values[^1]);
    }

    public static int NearestRank(IReadOnlyList<int> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) throw new ArgumentException("Values cannot be empty.", nameof(sortedValues));
        if (percentile is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(percentile));
        var rank = (int)Math.Ceiling(percentile * sortedValues.Count);
        return sortedValues[Math.Clamp(rank - 1, 0, sortedValues.Count - 1)];
    }
}
