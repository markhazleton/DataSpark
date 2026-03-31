using Microsoft.Data.Analysis;

namespace DataSpark.Core.Models.Analysis;

public static class BivariateAnalysisExtensions
{
    private static void AnalyzeColumnPair(this BivariateAnalysis analysis, DataFrameColumn column1, DataFrameColumn column2)
    {
        var values1 = column1.Cast<object>().ToArray();
        var values2 = column2.Cast<object>().ToArray();
        var uniqueValues1 = values1.Where(v => v != null).Distinct().ToArray();
        var uniqueValues2 = values2.Where(v => v != null).Distinct().ToArray();
        bool column1IsNumeric = AnalysisUtilities.IsNumericType(column1.DataType);
        bool column2IsNumeric = AnalysisUtilities.IsNumericType(column2.DataType);
        bool column1IsCategorical = AnalysisUtilities.IsCategorical(column1);
        bool column2IsCategorical = AnalysisUtilities.IsCategorical(column2);

        if (column1IsNumeric && column2IsNumeric)
        {
            double[] numericValues1 = uniqueValues1.OfType<IConvertible>().Select(Convert.ToDouble).ToArray();
            double[] numericValues2 = uniqueValues2.OfType<IConvertible>().Select(Convert.ToDouble).ToArray();
            if (numericValues1.Length == numericValues2.Length && numericValues1.Length > 0)
            {
                var correlation = AnalysisUtilities.CalculateCorrelation(numericValues1, numericValues2);
                var pValue = 0.05; // placeholder
                var noiseFactor1 = AnalysisUtilities.CalculateNoiseFactor(numericValues1);
                var noiseFactor2 = AnalysisUtilities.CalculateNoiseFactor(numericValues2);
                var outlierFactor1 = AnalysisUtilities.CalculateOutlierFactor(numericValues1);
                var outlierFactor2 = AnalysisUtilities.CalculateOutlierFactor(numericValues2);
                var combinedNoiseFactor = Math.Max(noiseFactor1, noiseFactor2) * Math.Max(outlierFactor1, outlierFactor2);
                analysis.InsightScore = AnalysisUtilities.CalculateInsightScore(correlation, uniqueValues1.Length, uniqueValues2.Length, column1.Length, pValue, combinedNoiseFactor);
                analysis.Observations.Add($"Correlation between '{analysis.Column1}' and '{analysis.Column2}' is {correlation:F2} with an insight score of {analysis.InsightScore:F2}%.");
                analysis.VisualizationRecommendations.Add("Recommended visualizations: scatter plots, heatmaps.");
            }
            else
            {
                analysis.Observations.Add("Unable to calculate correlation due to mismatched or insufficient data.");
                analysis.InsightScore = 0;
            }
        }
        else if (column1IsNumeric && column2IsCategorical)
        {
            analysis.Observations.Add($"Analyzing numeric '{analysis.Column1}' with categorical '{analysis.Column2}'.");
            analysis.InsightScore = 50;
            analysis.VisualizationRecommendations.Add("Recommended visualizations: box plots, bar charts with numeric values split by categories.");
        }
        else if (column1IsCategorical && column2IsNumeric)
        {
            analysis.Observations.Add($"Analyzing categorical '{analysis.Column1}' with numeric '{analysis.Column2}'.");
            analysis.InsightScore = 50;
            analysis.VisualizationRecommendations.Add("Recommended visualizations: box plots, bar charts with numeric values split by categories.");
        }
        else if (column1IsCategorical && column2IsCategorical)
        {
            var crossTab = values1.Zip(values2, (v1, v2) => new { Value1 = v1, Value2 = v2 })
                .GroupBy(x => new { x.Value1, x.Value2 })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());
            analysis.Observations.Add($"Cross-tabulation between '{analysis.Column1}' and '{analysis.Column2}' reveals the top 10 counts:");
            foreach (var entry in crossTab)
                analysis.Observations.Add($"{entry.Key.Value1} - {entry.Key.Value2}: {entry.Value}");
            analysis.InsightScore = 40;
            analysis.VisualizationRecommendations.Add("Recommended visualizations: mosaic plots, grouped bar charts.");
        }
        else
        {
            analysis.Observations.Add($"Column types are not suitable for paired analysis: '{column1.DataType}' vs '{column2.DataType}'.");
            analysis.InsightScore = 0;
        }
    }

    public static List<BivariateAnalysis> GetBivariateAnalysis(this DataFrame dataFrame)
    {
        var bivariateAnalyses = new List<BivariateAnalysis>();
        var columnPairs = from i in Enumerable.Range(0, dataFrame.Columns.Count)
                          from j in Enumerable.Range(i + 1, dataFrame.Columns.Count - i - 1)
                          select (dataFrame.Columns[i], dataFrame.Columns[j]);
        foreach (var pair in columnPairs)
        {
            var (column1, column2) = pair;
            var analysis = new BivariateAnalysis { Column1 = column1.Name, Column2 = column2.Name };
            try { analysis.AnalyzeColumnPair(column1, column2); }
            catch (Exception ex) { analysis.Observations.Add($"Error analyzing pair: {ex.Message}"); }
            bivariateAnalyses.Add(analysis);
        }
        return bivariateAnalyses;
    }
}
