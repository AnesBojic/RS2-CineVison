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
    /// Short labels so staff can find the rows that block a delete (including Pending holds
    /// and Cancelled tickets, which still count as history).
    /// </summary>
    public static async Task<List<string>> SummarizeBookingsAsync(
        CineVisionDbContext db,
        IReadOnlyCollection<int> projectionIds,
        int take = 8)
    {
        if (projectionIds.Count == 0)
        {
            return new List<string>();
        }

        var rows = await db.Reservations
            .AsNoTracking()
            .Where(r => projectionIds.Contains(r.ProjectionId))
            .OrderByDescending(r => r.ReservationDate)
            .Select(r => new
            {
                r.ReservationNumber,
                r.Status,
                r.CustomerName,
                r.CustomerEmail
            })
            .Take(take)
            .ToListAsync();

        return rows.Select(r =>
        {
            var who = !string.IsNullOrWhiteSpace(r.CustomerName)
                ? r.CustomerName
                : r.CustomerEmail;
            var suffix = string.IsNullOrWhiteSpace(who) ? string.Empty : $" — {who}";
            return $"{r.ReservationNumber} ({r.Status}){suffix}";
        }).ToList();
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
        return BuildImpact(id, displayName, bookingHistoryCount, null, parts);
    }

    public static CascadeDeleteImpactResponse BuildImpact(
        int id,
        string displayName,
        int bookingHistoryCount,
        IReadOnlyList<string>? bookingSummaries,
        params (string name, int count)[] parts)
    {
        var items = parts
            .Where(p => p.count > 0)
            .Select(p => new CascadeDeleteImpactItem { EntityName = p.name, Count = p.count })
            .ToList();

        string? blockReason = null;
        if (bookingHistoryCount > 0)
        {
            blockReason =
                $"{bookingHistoryCount} booking(s) exist for this record and cannot be erased. " +
                "Open Bookings, set the status filter to All statuses (Pending checkout holds " +
                "and Cancelled tickets also count), then search for the ticket. " +
                "Cancel those bookings to refund customers; the history stays permanently.";
            if (bookingSummaries is { Count: > 0 })
            {
                blockReason += " Found: " + string.Join("; ", bookingSummaries) + ".";
            }
        }

        return new CascadeDeleteImpactResponse
        {
            Id = id,
            DisplayName = displayName,
            TotalAffectedRows = items.Sum(i => i.Count),
            CanDelete = bookingHistoryCount == 0,
            BlockReason = blockReason,
            Items = items
        };
    }
}
