import '../core/enums/api_enums.dart';
import '../core/utils/utc_datetime.dart';

class ReservationSeat {
  final int id;
  final int seatId;
  final String rowLabel;
  final int seatNumber;
  final int seatType;
  final num price;

  ReservationSeat({
    required this.id,
    required this.seatId,
    required this.rowLabel,
    required this.seatNumber,
    required this.seatType,
    required this.price,
  });

  factory ReservationSeat.fromJson(Map<String, dynamic> json) {
    return ReservationSeat(
      id: json['id'] as int? ?? 0,
      seatId: json['seatId'] as int? ?? 0,
      rowLabel: json['rowLabel'] as String? ?? '',
      seatNumber: json['seatNumber'] as int? ?? 0,
      seatType: json['seatType'] as int? ?? 0,
      price: json['price'] as num? ?? 0,
    );
  }
}

class Reservation {
  final int id;
  final String reservationNumber;
  final DateTime reservationDate;
  final int status;
  final String statusName;
  final num totalAmount;
  final int userId;
  final String? customerName;
  final String? customerEmail;
  final int screeningId;
  final int movieId;
  final String movieTitle;
  final String hallName;
  final DateTime screeningStartTime;
  final DateTime screeningEndTime;
  final String? paymentTransactionId;
  final DateTime? paymentDate;
  final List<ReservationSeat> seats;

  Reservation({
    required this.id,
    required this.reservationNumber,
    required this.reservationDate,
    required this.status,
    required this.statusName,
    required this.totalAmount,
    required this.userId,
    this.customerName,
    this.customerEmail,
    required this.screeningId,
    required this.movieId,
    required this.movieTitle,
    required this.hallName,
    required this.screeningStartTime,
    required this.screeningEndTime,
    this.paymentTransactionId,
    this.paymentDate,
    this.seats = const [],
  });

  factory Reservation.fromJson(Map<String, dynamic> json) {
    final fallback = UtcDateTime.now();
    return Reservation(
      id: json['id'] as int? ?? 0,
      reservationNumber: json['reservationNumber'] as String? ?? '',
      reservationDate:
          UtcDateTime.tryParse(json['reservationDate']) ?? fallback,
      status: json['status'] as int? ?? 0,
      statusName: json['statusName'] as String? ?? '',
      totalAmount: json['totalAmount'] as num? ?? 0,
      userId: json['userId'] as int? ?? 0,
      customerName: json['customerName'] as String?,
      customerEmail: json['customerEmail'] as String?,
      screeningId: json['screeningId'] as int? ?? 0,
      movieId: json['movieId'] as int? ?? 0,
      movieTitle: json['movieTitle'] as String? ?? '',
      hallName: json['hallName'] as String? ?? '',
      screeningStartTime:
          UtcDateTime.tryParse(json['screeningStartTime']) ?? fallback,
      screeningEndTime: UtcDateTime.tryParse(json['screeningEndTime']) ??
          UtcDateTime.tryParse(json['screeningStartTime']) ??
          fallback,
      paymentTransactionId: json['paymentTransactionId'] as String?,
      paymentDate: UtcDateTime.tryParse(json['paymentDate']),
      seats: (json['seats'] as List<dynamic>?)
              ?.map((e) => ReservationSeat.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }

  bool get isPaidOrConfirmed =>
      status == ReservationStatus.confirmed || status == ReservationStatus.paid;

  bool get isCancelled => status == ReservationStatus.cancelled;

  bool get isPaid => status == ReservationStatus.paid;

  bool get isScreeningPast =>
      screeningEndTime.toUtc().isBefore(UtcDateTime.now());

  /// Refund/cancel only until 4 hours before the screening starts.
  bool get canRefund {
    if (!isPaidOrConfirmed || isCancelled) return false;
    final deadline =
        screeningStartTime.toUtc().subtract(const Duration(hours: 4));
    return UtcDateTime.now().isBefore(deadline);
  }
}
