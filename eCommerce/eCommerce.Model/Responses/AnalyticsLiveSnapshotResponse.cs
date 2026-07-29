using System;
using System.Collections.Generic;

namespace eCommerce.Model.Responses
{
    /// <summary>
    /// Full analytics payload pushed over SignalR when booking or review data changes.
    /// </summary>
    public class AnalyticsLiveSnapshotResponse
    {
        public DashboardResponse Dashboard { get; set; } = new();
        public List<MoviePerformanceResponse> MoviePerformance { get; set; } = new();
        public List<TimeSlotPerformanceResponse> TimeSlotPerformance { get; set; } = new();
        public List<HallUtilizationResponse> HallUtilization { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }
}
