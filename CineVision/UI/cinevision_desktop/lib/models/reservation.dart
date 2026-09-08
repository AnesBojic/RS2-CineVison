import 'package:cinevision_desktop/core/enums/api_enums.dart';
import 'package:cinevision_desktop/core/utils/utc_datetime.dart';

class Reservation {
  final int id;
  final String reservationNumber;
  final DateTime? reservationDate;
  final int status;
  final String statusName;
  final num totalAmount;
  final String? customerName;
  final String? customerEmail;
  final int projectionId;
  final String movieTitle;
  final String hallName;
  final DateTime? projectionStartTime;
  final int refundStatus;
  final String refundStatusName;
  final String? refundError;
  final DateTime? cancelledAt;
  final String? cancellationReason;
  final DateTime? holdExpiresAt;

  Reservation({
    required this.id,
    required this.reservationNumber,
    this.reservationDate,
    required this.status,
    required this.statusName,
    required this.totalAmount,
    this.customerName,
    this.customerEmail,
    required this.projectionId,
    required this.movieTitle,
    required this.hallName,
    this.projectionStartTime,
    this.refundStatus = RefundStatus.none,
    this.refundStatusName = '',
    this.refundError,
    this.cancelledAt,
    this.cancellationReason,
    this.holdExpiresAt,
  });

  factory Reservation.fromJson(Map<String, dynamic> json) {
    return Reservation(
      id: json['id'] as int? ?? 0,
      reservationNumber: json['reservationNumber'] as String? ?? '',
      reservationDate: UtcDateTime.tryParse(json['reservationDate']),
      status: json['status'] as int? ?? 0,
      statusName: json['statusName'] as String? ?? '',
      totalAmount: json['totalAmount'] as num? ?? 0,
      customerName: json['customerName'] as String?,
      customerEmail: json['customerEmail'] as String?,
      projectionId: json['projectionId'] as int? ?? 0,
      movieTitle: json['movieTitle'] as String? ?? '',
      hallName: json['hallName'] as String? ?? '',
      projectionStartTime: UtcDateTime.tryParse(json['projectionStartTime']),
      refundStatus: json['refundStatus'] as int? ?? RefundStatus.none,
      refundStatusName: json['refundStatusName'] as String? ?? '',
      refundError: json['refundError'] as String?,
      cancelledAt: UtcDateTime.tryParse(json['cancelledAt']),
      cancellationReason: json['cancellationReason'] as String?,
      holdExpiresAt: UtcDateTime.tryParse(json['holdExpiresAt']),
    );
  }

  bool get isCancelled => status == ReservationStatus.cancelled;

  bool get canRetryRefund => refundStatus == RefundStatus.failed;

  String get refundLabel {
    return switch (refundStatus) {
      RefundStatus.refunded => 'Refunded',
      RefundStatus.failed => 'Failed',
      RefundStatus.pending => 'Pending',
      _ => '—',
    };
  }

  String get reasonLabel {
    final text = cancellationReason?.trim();
    if (text != null && text.isNotEmpty) return text;
    if (status == ReservationStatus.pending && holdExpiresAt != null) {
      return 'Checkout hold';
    }
    return isCancelled ? 'No reason recorded' : '—';
  }
}
