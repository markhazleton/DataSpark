namespace Sql2Csv.Web.ViewModels;

public sealed class CoverageMetrics
{
    public string? Percent { get; set; }
    public int? LinesCovered { get; set; }
    public int? LinesTotal { get; set; }
    public int? BranchesCovered { get; set; }
    public int? BranchesTotal { get; set; }
    public int? FilesCovered { get; set; }
    public int? FilesTotal { get; set; }
    public int? ClassesCovered { get; set; }
    public int? ClassesTotal { get; set; }
    public int? MethodsCovered { get; set; }
    public int? MethodsTotal { get; set; }
    public int? StatementsCovered { get; set; }
    public int? StatementsTotal { get; set; }
    public int? MissedLines { get; set; }
    public int? MissedBranches { get; set; }
    public int? MissedFiles { get; set; }
    public int? MissedClasses { get; set; }
    public int? MissedMethods { get; set; }
    public int? MissedStatements { get; set; }
    public string? Source { get; set; } // xml or json
}
