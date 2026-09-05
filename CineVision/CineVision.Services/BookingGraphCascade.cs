using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model.Exceptions;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineVision.Services;

/// <summary>
/// Guards deletes that would reach into the booking graph. A projection that was ever used in a
/// reservation is never removed: deleting it would erase the financial and seat record. Hard
/// delete stays only for projections that never had a booking at all.
/// </summary>
internal static class BookingGraphCascade
{
    public sealed record ProjectionGraphCounts(
        int ProjectionCount,
        int ReservationCount,
        int ReservationSeatCount);

    public static async Task<ProjectionGraphCounts> CountForProjectionIdsAsync(
        CineVisionDbContext db,
        IReadOnlyCollection<int> projectionIds)
    {
        if (projectionIds.Count == 0)
        {
            return new ProjectionGraphCounts(0, 0, 0);
        }

        var reservationCount = await db.Reservations
            .CountAsync(r => projectionIds.Contains(r.ProjectionId));
        var seatCount = await db.ReservationSeats
            .CountAsync(rs => projectionIds.Contains(rs.ProjectionId) && rs.ReleasedAt == null);

        return new ProjectionGraphCounts(projectionIds.Count, reservationCount, seatCount);
    }

    /// <summary>
    /// Number of bookings attached to these projections. Any of them makes the projection undeletable.
    /// </summary>
    public static async Task<int> CountBookingHistoryAsync(
        CineVisionDbContext db,
        IReadOnlyCollection<int> projectionIds)
    {
        if (projectionIds.Count == 0)
        {
            return 0;
        }

        return await db.Reservations
            .CountAsync(r => projectionIds.Contains(r.ProjectionId));
    }

    /// <summary>
    /// Deletes projections that have never been used in a booking. Throws when any reservation
    /// exists — cancelled, paid, counter, or abandoned hold — so the record stays permanent.
    /// </summary>
    public static async Task RemoveProjectionsAsync(
        CineVisionDbContext db,
        IReadOnlyCollection<int> projectionIds,
        string subject)
    {
        if (projectionIds.Count == 0)
        {
            return;
        }

        var reservationCount = await db.Reservations
            .CountAsync(r => projectionIds.Contains(r.ProjectionId));
        if (reservationCount > 0)
        {
            throw new ClientException(
                $"{subject} has {reservationCount} booking(s) and cannot be deleted. " +
                "Cancel individual bookings to refund customers; the booking record is kept permanently.");
        }

        var projections = await db.Projections
            .Where(s => projectionIds.Contains(s.Id))
            .ToListAsync();
        if (projections.Count > 0)
        {
            db.Projections.RemoveRange(projections);
        }
    }

    public static CascadeDeleteImpactResponse BuildImpact(
        int id,
        string displayName,
        int bookingHistoryCount,
        params (string name, int count)[] parts)
    {
        var items = parts
            .Where(p => p.count > 0)
            .Select(p => new CascadeDeleteImpactItem { EntityName = p.name, Count = p.count })
            .ToList();

        return new CascadeDeleteImpactResponse
        {
            Id = id,
            DisplayName = displayName,
            TotalAffectedRows = items.Sum(i => i.Count),
            CanDelete = bookingHistoryCount == 0,
            BlockReason = bookingHistoryCount == 0
                ? null
                : $"{bookingHistoryCount} booking(s) exist for this record and cannot be erased. " +
                  "Cancel those bookings to refund customers; the history stays permanently.",
            Items = items
        };
    }
}
