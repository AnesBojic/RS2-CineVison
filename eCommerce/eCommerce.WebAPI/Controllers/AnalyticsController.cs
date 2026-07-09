using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Reporting endpoints backing the desktop dashboard and analytics screens.
/// Restricted to management roles.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin,Staff")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>Key indicators for the dashboard: revenue, tickets, occupancy and top movies.</summary>
    [HttpGet("Dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard()
    {
        return Ok(await _analyticsService.GetDashboardAsync());
    }

    /// <summary>Sales and occupancy per movie, optionally limited to a date range.</summary>
    [HttpGet("MoviePerformance")]
    public async Task<ActionResult<List<MoviePerformanceResponse>>> GetMoviePerformance([FromQuery] ReportSearchObject? search)
    {
        return Ok(await _analyticsService.GetMoviePerformanceAsync(search));
    }

    /// <summary>Revenue and ticket counts bucketed by day, week or month.</summary>
    [HttpGet("RevenueByPeriod")]
    public async Task<ActionResult<List<RevenueByPeriodResponse>>> GetRevenueByPeriod([FromQuery] ReportSearchObject? search)
    {
        return Ok(await _analyticsService.GetRevenueByPeriodAsync(search));
    }

    /// <summary>How intensively each hall is used across its screenings.</summary>
    [HttpGet("HallUtilization")]
    public async Task<ActionResult<List<HallUtilizationResponse>>> GetHallUtilization([FromQuery] ReportSearchObject? search)
    {
        return Ok(await _analyticsService.GetHallUtilizationAsync(search));
    }

    /// <summary>Tickets, occupancy and revenue bucketed into fixed daily time slots.</summary>
    [HttpGet("PerformanceByTimeSlot")]
    public async Task<ActionResult<List<TimeSlotPerformanceResponse>>> GetPerformanceByTimeSlot([FromQuery] ReportSearchObject? search)
    {
        return Ok(await _analyticsService.GetPerformanceByTimeSlotAsync(search));
    }
}
