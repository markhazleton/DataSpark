using Microsoft.Data.Analysis;

namespace DataSpark.Core.Models.Analysis;

public static class AnalysisUtilities
{
    public static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    public static double CalculateStandardDeviation(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Length;
        return Math.Sqrt(variance);
    }

    public static double CalculateSkewness(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values);
        if (stdDev == 0) return 0;
        return values.Sum(v => Math.Pow((v - mean) / stdDev, 3)) / values.Length;
    }

    public static double CalculateMedian(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        var sorted = values.OrderBy(v => v).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2d
            : sorted[mid];
    }

    public static double CalculatePercentile(double[] values, double percentile)
    {
        if (values.Length == 0) return double.NaN;
        if (percentile < 0 || percentile > 1) throw new ArgumentOutOfRangeException(nameof(percentile));

        var sorted = values.OrderBy(v => v).ToArray();
        var rank = (sorted.Length - 1) * percentile;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var weight = rank - lowerIndex;
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * weight;
    }

    public static double CalculateKurtosis(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        var mean = values.Average();
        var stdDev = CalculateStandardDeviation(values);
        if (stdDev == 0) return 0;

        return values.Sum(v => Math.Pow((v - mean) / stdDev, 4)) / values.Length - 3.0;
    }

    public static double CalculateCorrelation(double[] values1, double[] values2)
    {
        var mean1 = values1.Average();
        var mean2 = values2.Average();
        var sumProduct = values1.Zip(values2, (v1, v2) => (v1 - mean1) * (v2 - mean2)).Sum();
        var sumSquare1 = values1.Sum(v => Math.Pow(v - mean1, 2));
        var sumSquare2 = values2.Sum(v => Math.Pow(v - mean2, 2));
        return sumProduct / Math.Sqrt(sumSquare1 * sumSquare2);
    }

    public static double CalculateNoiseFactor(double[] values)
    {
        if (values.Length == 0) return 1.0;
        double mean = values.Average();
        double stdDev = CalculateStandardDeviation(values);
        double noiseRatio = mean != 0 ? stdDev / Math.Abs(mean) : 1.0;
        double noiseThreshold = 0.5;
        double excessiveNoiseThreshold = 1.0;
        if (noiseRatio > excessiveNoiseThreshold) return 1.0;
        if (noiseRatio > noiseThreshold) return 0.75 + 0.25 * (noiseRatio - noiseThreshold) / (excessiveNoiseThreshold - noiseThreshold);
        return Math.Clamp(1.0 - noiseRatio, 0.0, 1.0);
    }

    public static double CalculateOutlierFactor(double[] values)
    {
        if (values.Length == 0) return 1.0;
        double mean = values.Average();
        double stdDev = CalculateStandardDeviation(values);
        double lowerBound = mean - 3 * stdDev;
        double upperBound = mean + 3 * stdDev;
        int outlierCount = values.Count(v => v < lowerBound || v > upperBound);
        double outlierRatio = (double)outlierCount / values.Length;
        return Math.Clamp(1.0 - outlierRatio, 0.0, 1.0);
    }

    public static double CalculateInsightScore(double correlation, int uniqueCount1, int uniqueCount2, long totalCount, double pValue = 1.0, double noiseFactor = 0.0)
    {
        double scaledCorrelation = Math.Abs(correlation);
        double pValueAdjustment = Math.Max(0.1, 1 - pValue);
        double uniquenessScore = 1.0 - Math.Min((double)(uniqueCount1 * uniqueCount2) / (totalCount * totalCount), 1.0);
        double noisePenalty = Math.Clamp(1.0 - noiseFactor, 0.0, 1.0);
        double combinedScore = 0.5 * scaledCorrelation + 0.3 * pValueAdjustment + 0.2 * uniquenessScore;
        double insightScore = combinedScore * noisePenalty;
        return Math.Clamp(insightScore, 0.0, 1.0) * 100;
    }

    public static bool IsCategorical(DataFrameColumn column, int threshold = 20)
    {
        var columnType = column.DataType;
        if (columnType == typeof(string) || columnType == typeof(bool)) return true;
        if (IsNumericType(columnType))
        {
            var uniqueValueCount = column.Cast<object>().Distinct().Count();
            var totalValues = column.Length;
            double uniqueRatio = (double)uniqueValueCount / totalValues;
            if (uniqueValueCount <= threshold || uniqueRatio < 0.1) return true;
        }
        if (columnType.IsEnum) return true;
        if (columnType == typeof(object))
        {
            var uniqueValues = column.Cast<object>().Where(value => value != null).Distinct().ToArray();
            if (uniqueValues.Length <= threshold) return true;
        }
        return false;
    }
}
