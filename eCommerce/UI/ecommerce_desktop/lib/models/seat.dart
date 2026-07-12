class Seat {
  final int? id;
  final int? hallId;
  final String? rowLabel;
  final int? seatNumber;
  final int? seatType;
  final int? partnerSeatId;
  final int? spotsOccupied;
  final bool? isActive;

  Seat({
    this.id,
    this.hallId,
    this.rowLabel,
    this.seatNumber,
    this.seatType,
    this.partnerSeatId,
    this.spotsOccupied,
    this.isActive,
  });

  factory Seat.fromJson(Map<String, dynamic> json) {
    return Seat(
      id: json['id'] as int?,
      hallId: json['hallId'] as int?,
      rowLabel: json['rowLabel'] as String?,
      seatNumber: json['seatNumber'] as int?,
      seatType: json['seatType'] as int?,
      partnerSeatId: json['partnerSeatId'] as int?,
      spotsOccupied: json['spotsOccupied'] as int?,
      isActive: json['isActive'] as bool?,
    );
  }

  bool get isCouple => seatType == 2;

  int get normalizedType => seatType == 2 ? 2 : 0;
}

const seatTypeLabels = ['Regular', 'Couple'];
