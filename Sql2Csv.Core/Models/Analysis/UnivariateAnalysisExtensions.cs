using Microsoft.Data.Analysis;
using System.Collections.Concurrent;

namespace Sql2Csv.Core.Models.Analysis;

public static class UnivariateAnalysisExtensions
{
    public static List<ColumnInfo> GetUnivariateAnalysis(this DataFrame dataFrame, AnalysisConfig? config = null)
    {
        config ??= new AnalysisConfig();
        var columnInformationList = new ConcurrentBag<ColumnInfo>();

        Parallel.ForEach(dataFrame.Columns, column =>
        {
            try { columnInformationList.Add(GetColumnAnalysis(config, column)); }
            catch (Exception) { /* Non-fatal: individual column analysis failures do not bubble up. */ }
        });
        return [.. columnInformationList];
    }

    private static ColumnInfo GetColumnAnalysis(AnalysisConfig config, DataFrameColumn column)
    {
        var values = column.Cast<object>().ToArray();
        var nonNullValues = values.Where(value => value != null).ToArray();
        var uniqueValues = nonNullValues.Distinct().ToArray();
        var mostCommonValue = nonNullValues.GroupBy(x => x).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

        var numericValues = nonNullValues
            .Where(v => v is byte or short or int or long or float or double or decimal)
            .Select(Convert.ToDouble)
            .ToArray();

        var mean = numericValues.Length > 0 ? numericValues.Average() : double.NaN;
        var standardDeviation = numericValues.Length > 0 ? AnalysisUtilities.CalculateStandardDeviation(numericValues) : double.NaN;
        var skewness = numericValues.Length > 0 ? AnalysisUtilities.CalculateSkewness(numericValues) : double.NaN;

        var columnInfo = new ColumnInfo
        {
            Column = column.Name,
            Type = column.DataType.ToString(),
            NonNullCount = nonNullValues.Length,
            NullCount = values.Length - nonNullValues.Length,
            UniqueCount = uniqueValues.Length,
            MostCommonValue = mostCommonValue,
            Skewness = skewness,
            Min = numericValues.Length > 0 ? numericValues.Min() : null,
            Max = numericValues.Length > 0 ? numericValues.Max() : null,
            Mean = mean,
            StandardDeviation = standardDeviation
        };

        AnalyzeColumn(columnInfo, nonNullValues, numericValues, config);
        return columnInfo;
    }

    private static void AnalyzeColumn(ColumnInfo column, object[] nonNullValues, double[] numericValues, AnalysisConfig config)
    {
        bool isNumeric = numericValues.Length > 0;
        bool isCategorical = column.Type == typeof(string).FullName;

        if (isNumeric)
        {
            column.Observations.Add($"The column '{column.Column}' is a numeric type, suitable for quantitative analysis.");
            if (Math.Abs(column.Skewness) > config.HighSkewnessThreshold)
                column.Observations.Add($"The column exhibits a high skewness of {column.Skewness:F2}. Consider transformations to normalize the data.");
            else if (Math.Abs(column.Skewness) > config.ModerateSkewnessThreshold)
                column.Observations.Add($"Moderate skewness detected ({column.Skewness:F2}). May slightly affect tests assuming normality.");
            else
                column.Observations.Add($"Skewness of {column.Skewness:F2} indicates symmetric distribution, favorable for analysis.");

            if (column.UniqueCount == column.NonNullCount)
                column.Observations.Add("All values are unique. Recommended visualizations: scatter plots and line plots.");
            else if (column.UniqueCount > config.NonUniqueThresholdForHistograms)
                column.Observations.Add("Values are mostly unique. Recommended visualizations: scatter plots, line plots, or density plots.");
            else if (column.UniqueCount > config.NonUniqueThresholdForBoxPlots)
                column.Observations.Add("Values have moderate uniqueness. Recommended visualizations: box plots, histograms, and violin plots.");
            else
                column.Observations.Add("Repeated values detected. Recommended visualizations: histograms, box plots, and bar charts.");

            if (column.UniqueCount < column.NonNullCount)
                column.Observations.Add($"Repeated values detected. {column.NonNullCount - column.UniqueCount} duplicates found.");
            else
                column.Observations.Add("All values are unique, suggesting potential identifiers.");

            if (column.StandardDeviation > 0 && column.Min != null && column.Max != null)
            {
                double upperThreshold = column.Mean + config.OutlierStdDevMultiplier * column.StandardDeviation;
                double lowerThreshold = column.Mean - config.OutlierStdDevMultiplier * column.StandardDeviation;
                if (Convert.ToDouble(column.Max) > upperThreshold)
                    column.Observations.Add("Potential high outliers detected above the specified threshold.");
                if (Convert.ToDouble(column.Min) < lowerThreshold)
                    column.Observations.Add("Potential low outliers detected below the specified threshold.");
                if (Convert.ToDouble(column.Max) <= upperThreshold && Convert.ToDouble(column.Min) >= lowerThreshold)
                    column.Observations.Add("No significant outliers detected.");
            }
            else
            {
                column.Observations.Add("Insufficient data for outlier detection.");
            }

            column.Observations.Add($"Mean value is {column.Mean:F2}. This represents the central tendency of the data.");
            column.Observations.Add($"Standard deviation is {column.StandardDeviation:F2}, indicating the spread or dispersion of the data values.");
        }
        else if (isCategorical)
        {
            column.Observations.Add($"The column '{column.Column}' is a categorical type, ideal for grouping and segmentation.");
            column.Observations.Add("Recommended visualizations: bar charts, pie charts, and count plots.");
            if (column.UniqueCount < column.NonNullCount)
                column.Observations.Add($"Repeated values detected. {column.NonNullCount - column.UniqueCount} duplicates found.");
            else
                column.Observations.Add("All values are unique, suggesting potential identifiers.");
            column.Observations.Add($"Most common value is '{column.MostCommonValue}', which appears frequently in the dataset.");
            column.Observations.Add($"The column contains {column.UniqueCount} unique categories.");
            if (column.UniqueCount < config.UniqueCountThreshold)
            {
                var valueCounts = nonNullValues.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => $"{g.Key}: {g.Count()}").ToList();
                column.Observations.Add("Values (in descending order of counts): " + string.Join(", ", valueCounts));
            }
        }
        else
        {
            column.Observations.Add($"Column '{column.Column}' of type '{column.Type}' is not commonly analyzed directly. Consider feature extraction.");
        }

        if (column.NullCount > 0)
            column.Observations.Add($"The column contains {column.NullCount} missing values. Consider handling these before analysis.");
        else
            column.Observations.Add("No missing values detected.");

        if (column.UniqueCount < column.NonNullCount)
            column.Observations.Add($"Detected {column.NonNullCount - column.UniqueCount} duplicate values.");

        if (column.Type == typeof(DateTime).FullName)
            column.Observations.Add($"The column '{column.Column}' is a DateTime type. Time series analysis and trends over time could be valuable.");

        if (column.NonNullCount == 0)
            column.Observations.Add("The column contains no non-null values, making it unsuitable for analysis.");
    }
}
