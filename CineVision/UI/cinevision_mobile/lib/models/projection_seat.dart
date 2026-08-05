import '../core/enums/api_enums.dart';

class ProjectionSeat {
  final int seatId;
  final int hallId;
  final String rowLabel;
  final int seatNumber;
  final int seatType;
  final int? partnerSeatId;
  final int spotsOccupied;
  final bool isTaken;
  final num price;

  ProjectionSeat({
    required this.seatId,
    required this.hallId,
    required this.rowLabel,
    required this.seatNumber,
    required this.seatType,
    this.partnerSeatId,
    this.spotsOccupied = 1,
    this.isTaken = false,
    this.price = 0,
  });

  factory ProjectionSeat.fromJson(Map<String, dynamic> json) {
    return ProjectionSeat(
      seatId: json['seatId'] as int,
      hallId: json['hallId'] as int,
      rowLabel: json['rowLabel'] as String? ?? '',
      seatNumber: json['seatNumber'] as int? ?? 0,
      seatType: json['seatType'] as int? ?? 0,
      partnerSeatId: json['partnerSeatId'] as int?,
      spotsOccupied: json['spotsOccupied'] as int? ?? 1,
      isTaken: json['isTaken'] as bool? ?? false,
      price: json['price'] as num? ?? 0,
    );
  }

  bool get isCouple => seatType == SeatTypes.couple;

  String get label => '$rowLabel$seatNumber';
}
