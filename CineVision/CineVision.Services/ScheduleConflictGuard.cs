using System;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Exceptions;
using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services;

/// <summary>
/// Shared hall-overlap checks so inserting a projection and later changing a movie's duration
/// cannot silently produce two shows in the same hall at the same time.
/// Cancelled projections do not occupy the hall.
/// </summary>
internal static class ScheduleConflictGuard
{
    public static async Task EnsureNoHallOverlapAsync(
        CineVisionDbContext db,
        int hallId,
        DateTime start,
        DateTime end,
        int? excludeProjectionId = null)
    {
        if (end <= start)
        {
            throw new ClientException("Projection end time must be after start time.");
        }

        var query = db.Projections.AsNoTracking()
            .Where(s =>
                s.CancelledAt == null &&
                s.HallId == hallId &&
                s.StartTime < end &&
                s.StartTime.AddMinutes(s.Movie.DurationMinutes) > start);

        if (excludeProjectionId.HasValue)
        {
            query = query.Where(s => s.Id != excludeProjectionId.Value);
        }

        var conflict = await query
            .Select(s => new
            {
                s.Id,
                s.StartTime,
                EndTime = s.StartTime.AddMinutes(s.Movie.DurationMinutes)
            })
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            throw new ClientException(
                $"Hall already has projection #{conflict.Id} from {conflict.StartTime:u} to {conflict.EndTime:u} (UTC). Choose another time or hall.");
        }
    }

    /// <summary>
    /// Re-checks every still-scheduled projection of a movie against the proposed duration.
    /// Sibling projections of the same movie are evaluated with the new length, not the stored one.
    /// </summary>
    public static async Task EnsureMovieDurationFitsAsync(
        CineVisionDbContext db,
        int movieId,
        int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ClientException("Duration must be greater than zero.");
        }

        var own = await db.Projections.AsNoTracking()
            .Where(s => s.MovieId == movieId && s.CancelledAt == null)
            .Select(s => new { s.Id, s.HallId, s.StartTime })
            .ToListAsync();

        if (own.Count == 0)
        {
            return;
        }

        var hallIds = own.Select(s => s.HallId).Distinct().ToList();
        var occupants = await db.Projections.AsNoTracking()
            .Where(s => hallIds.Contains(s.HallId) && s.CancelledAt == null)
            .Select(s => new
            {
                s.Id,
                s.HallId,
                s.MovieId,
                s.StartTime,
                s.Movie.DurationMinutes
            })
            .ToListAsync();

        foreach (var projection in own)
        {
            var end = projection.StartTime.AddMinutes(durationMinutes);
            if (end <= projection.StartTime)
            {
                throw new ClientException("Projection end time must be after start time.");
            }

            var conflict = occupants.FirstOrDefault(other =>
                other.HallId == projection.HallId &&
                other.Id != projection.Id &&
                other.StartTime < end &&
                other.StartTime.AddMinutes(
                    other.MovieId == movieId ? durationMinutes : other.DurationMinutes) > projection.StartTime);

            if (conflict != null)
            {
                var conflictEnd = conflict.StartTime.AddMinutes(
                    conflict.MovieId == movieId ? durationMinutes : conflict.DurationMinutes);
                throw new ClientException(
                    $"Changing this movie's duration would overlap projection #{projection.Id} with #{conflict.Id} " +
                    $"({conflict.StartTime:u}–{conflictEnd:u} UTC) in the same hall. Cancel or reschedule those shows first.");
            }
        }
    }
}
