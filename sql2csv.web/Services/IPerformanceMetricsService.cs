using Sql2Csv.Web.ViewModels;

namespace Sql2Csv.Web.Services;

public interface IPerformanceMetricsService
{
    Task<PerformanceMetricsViewModel> GetMetricsAsync(CancellationToken cancellationToken = default);
}
