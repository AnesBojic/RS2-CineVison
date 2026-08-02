using eCommerce.Model.Exceptions;
using eCommerce.Services.Database;
using eCommerce.Model.Enums;

namespace eCommerce.Services.ReservationStateMachine;

/// <summary>Allowed <see cref="ReservationStatus"/> transitions; use <see cref="Apply"/> for all changes.</summary>
public static class ReservationStatusTransitions
{
    private static readonly Dictionary<ReservationStatus, HashSet<ReservationStatus>> Allowed = new()
    {
        [ReservationStatus.Pending] = new()
        {
            ReservationStatus.Confirmed,
            ReservationStatus.Paid,
            ReservationStatus.Cancelled
        },
        [ReservationStatus.Confirmed] = new()
        {
            ReservationStatus.Paid,
            ReservationStatus.Cancelled,
            ReservationStatus.Completed
        },
        [ReservationStatus.Paid] = new()
        {
            ReservationStatus.Cancelled,
            ReservationStatus.Completed
        },
        [ReservationStatus.Cancelled] = new(),
        [ReservationStatus.Completed] = new()
    };

    /// <summary>Initial statuses assigned when a reservation row is created.</summary>
    public static bool IsValidInitialStatus(ReservationStatus status) =>
        status is ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.Paid;

    public static bool CanTransition(ReservationStatus from, ReservationStatus to) =>
        from == to || (Allowed.TryGetValue(from, out var next) && next.Contains(to));

    public static void EnsureCanTransition(ReservationStatus from, ReservationStatus to)
    {
        if (from == to)
        {
            return;
        }

        if (!CanTransition(from, to))
        {
            throw new ClientException(
                $"Reservation status cannot change from {from} to {to}.");
        }
    }

    /// <summary>
    /// Applies a status transition and fills related audit timestamps.
    /// </summary>
    public static void Apply(
        Reservation reservation,
        ReservationStatus to,
        int? cancelledByUserId = null,
        string? cancellationReason = null)
    {
        EnsureCanTransition(reservation.Status, to);

        if (reservation.Status == to)
        {
            return;
        }

        reservation.Status = to;

        if (to == ReservationStatus.Cancelled)
        {
            reservation.CancelledAt ??= DateTime.UtcNow;
            if (cancelledByUserId.HasValue)
            {
                reservation.CancelledByUserId = cancelledByUserId;
            }

            if (!string.IsNullOrWhiteSpace(cancellationReason))
            {
                reservation.CancellationReason = cancellationReason.Trim();
            }
            else if (string.IsNullOrWhiteSpace(reservation.CancellationReason))
            {
                reservation.CancellationReason = "Cancelled";
            }
        }

        if (to == ReservationStatus.Completed)
        {
            reservation.CompletedAt ??= DateTime.UtcNow;
        }

        if (to == ReservationStatus.Paid)
        {
            reservation.PaymentDate ??= DateTime.UtcNow;
        }
    }
}
