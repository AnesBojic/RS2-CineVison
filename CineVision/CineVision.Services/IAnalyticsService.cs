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

        /// <summary>
        /// Drops the in-memory snapshot so the next read (HTTP or SignalR) is rebuilt from the database.
        /// </summary>
        void InvalidateSnapshot();
    }
}
