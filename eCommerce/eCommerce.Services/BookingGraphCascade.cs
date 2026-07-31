using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services;

/// <summary>
/// Shared cascade helpers for deleting screenings and their booking graph (children first).
/// </summary>
internal static class BookingGraphCascade
{
    public sealed record ScreeningGraphCounts(
        int ScreeningCount,
        int ReservationCount,
        int ReservationSeatCount);

    public static async Task<ScreeningGraphCounts> CountForScreeningIdsAsync(
        ECommerceDbContext db,
        IReadOnlyCollection<int> screeningIds)
    {
        if (screeningIds.Count == 0)
        {
            return new ScreeningGraphCounts(0, 0, 0);
        }

        var reservationCount = await db.Reservations
            .CountAsync(r => screeningIds.Contains(r.ScreeningId));
        var seatCount = await db.ReservationSeats
            .CountAsync(rs => screeningIds.Contains(rs.ScreeningId));

        return new ScreeningGraphCounts(screeningIds.Count, reservationCount, seatCount);
    }

    /// <summary>
    /// Refunds paid bookings when possible, then hard-deletes reservation seats, reservations, and screenings.
    /// Caller owns the transaction / SaveChanges.
    /// </summary>
    public static async Task RemoveScreeningsAsync(
        ECommerceDbContext db,
        IReadOnlyCollection<int> screeningIds,
        Func<string, Task>? tryRefundPaidAsync = null)
    {
        if (screeningIds.Count == 0)
        {
            return;
        }

        var reservations = await db.Reservations
            .Where(r => screeningIds.Contains(r.ScreeningId))
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
                .Where(rs => screeningIds.Contains(rs.ScreeningId))
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

        var screenings = await db.Screenings
            .Where(s => screeningIds.Contains(s.Id))
            .ToListAsync();
        if (screenings.Count > 0)
        {
            db.Screenings.RemoveRange(screenings);
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
