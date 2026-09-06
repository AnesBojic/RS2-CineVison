using System.Collections.Generic;
using System.Linq;
using CineVision.Model.Enums;
using CineVision.Services.Database;

namespace CineVision.Services;

/// <summary>
/// Physical people-capacity of a seat layout. ReservationSeat rows are one per physical
/// spot (a couple booking writes two rows), so occupancy and availability must use the
/// same unit: regular active = 1, couple primary = 2, inactive partner = 0.
/// </summary>
internal static class SeatCapacity
{
    public const int RegularSpots = 1;
    public const int CoupleSpots = 2;

    public static int PhysicalSpots(bool isActive, SeatType seatType)
    {
        if (!isActive)
        {
            return 0;
        }

        return seatType == SeatType.Couple ? CoupleSpots : RegularSpots;
    }

    public static int PhysicalSpots(Seat seat) => PhysicalSpots(seat.IsActive, seat.SeatType);

    public static int Of(IEnumerable<Seat> seats) => seats.Sum(PhysicalSpots);
}
