using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineVision.Model;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using Microsoft.EntityFrameworkCore;
using CineVision.Model.Enums;

namespace CineVision.Services;

/// <summary>
/// Shared cascade helpers for deleting projections and their booking graph (children first).
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
            .CountAsync(rs => projectionIds.Contains(rs.ProjectionId));

        return new ProjectionGraphCounts(projectionIds.Count, reservationCount, seatCount);
    }

    /// <summary>
    /// Refunds paid bookings when possible, then hard-deletes reservation seats, reservations, and projections.
    /// Caller owns the transaction / SaveChanges.
    /// </summary>
    public static async Task RemoveProjectionsAsync(
        CineVisionDbContext db,
        IReadOnlyCollection<int> projectionIds,
        Func<string, Task>? tryRefundPaidAsync = null)
    {
        if (projectionIds.Count == 0)
        {
            return;
        }

        var reservations = await db.Reservations
            .Where(r => projectionIds.Contains(r.ProjectionId))
            .Include(r => r.ReservationSeats)
            .ToListAsync();

        if (tryRefundPaidAsync != null)
        {
            foreach (var reservation in reservations)
            {
                if (reservation.Status == ReservationStatus.Paid &&
                    !string.IsNullOrWhiteSpace(reservation.PaymentTransactionId))
                {
                    await tryRefundPaidAsync(reservation.PaymentTransactionId);
                }
            }
        }

        var seatRows = reservations.SelectMany(r => r.ReservationSeats).ToList();
        if (seatRows.Count == 0)
        {
            seatRows = await db.ReservationSeats
                .Where(rs => projectionIds.Contains(rs.ProjectionId))
                .ToListAsync();
        }

        if (seatRows.Count > 0)
        {
            db.ReservationSeats.RemoveRange(seatRows);
        }

        if (reservations.Count > 0)
        {
            db.Reservations.RemoveRange(reservations);
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
            Items = items
        };
    }
}
