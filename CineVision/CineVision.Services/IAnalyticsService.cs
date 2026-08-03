using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardResponse> GetDashboardAsync();
        Task<List<MoviePerformanceResponse>> GetMoviePerformanceAsync(ReportSearchObject? search);
        Task<List<RevenueByPeriodResponse>> GetRevenueByPeriodAsync(ReportSearchObject? search);
        Task<List<HallUtilizationResponse>> GetHallUtilizationAsync(ReportSearchObject? search);
        Task<List<TimeSlotPerformanceResponse>> GetPerformanceByTimeSlotAsync(ReportSearchObject? search);
        Task<AnalyticsLiveSnapshotResponse> GetLiveSnapshotAsync();
    }
}
