namespace DataSpark.Web.Models;

/// <summary>
/// Web-specific view model for the Univariate analysis page (presentation-only concerns).
/// </summary>
public class UnivariateViewModel
{
    public string FileName { get; set; } = string.Empty;
    public List<string> AvailableColumns { get; set; } = [];
    public string SelectedColumn { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

