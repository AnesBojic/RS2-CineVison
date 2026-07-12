import 'package:ecommerce_desktop/models/seat.dart';
import 'package:flutter/material.dart';

class Hall {
  final int? id;
  final String? name;
  final String? description;
  final int? screenType;
  final String? screenTypeName;
  final int? status;
  final String? statusName;
  final bool? isActive;
  final int? seatCount;
  final int? capacity;
  final int? rowCount;
  final int? seatsPerRow;
  final List<Seat> seats;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  Hall({
    this.id,
    this.name,
    this.description,
    this.screenType,
    this.screenTypeName,
    this.status,
    this.statusName,
    this.isActive,
    this.seatCount,
    this.capacity,
    this.rowCount,
    this.seatsPerRow,
    this.seats = const [],
    this.createdAt,
    this.updatedAt,
  });

  factory Hall.fromJson(Map<String, dynamic> json) {
    final seatsJson = json['seats'] as List<dynamic>?;
    return Hall(
      id: json['id'] as int?,
      name: json['name'] as String?,
      description: json['description'] as String?,
      screenType: json['screenType'] as int?,
      screenTypeName: json['screenTypeName'] as String?,
      status: json['status'] as int?,
      statusName: json['statusName'] as String?,
      isActive: json['isActive'] as bool?,
      seatCount: json['seatCount'] as int?,
      capacity: json['capacity'] as int?,
      rowCount: json['rowCount'] as int?,
      seatsPerRow: json['seatsPerRow'] as int?,
      seats: seatsJson?.map((e) => Seat.fromJson(e as Map<String, dynamic>)).toList() ?? const [],
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString())
          : null,
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString())
          : null,
    );
  }

  Map<String, dynamic> toInsertJson({
    required int rowsCount,
    required int seatsPerRow,
  }) =>
      {
        'name': name,
        'description': description ?? '',
        'screenType': screenType ?? 0,
        'status': status ?? 0,
        'isActive': isActive ?? true,
        'rowsCount': rowsCount,
        'seatsPerRow': seatsPerRow,
      };

  Map<String, dynamic> toUpdateJson() => {
        'name': name,
        'description': description ?? '',
        'screenType': screenType ?? 0,
        'status': status ?? 0,
        'isActive': isActive ?? true,
      };
}

String hallLayoutLabel(Hall hall) {
  final rows = hall.rowCount ?? derivedRowCount(hall);
  final cols = hall.seatsPerRow ?? derivedSeatsPerRow(hall);
  if (rows > 0 && cols > 0) return '$rows × $cols';
  return '${hall.capacity ?? hall.seatCount ?? 0} seats';
}

int derivedRowCount(Hall hall) {
  if (hall.seats.isEmpty) return 0;
  return hall.seats.map((s) => s.rowLabel).toSet().length;
}

int derivedSeatsPerRow(Hall hall) {
  if (hall.seats.isEmpty) return 0;
  final byRow = <String, int>{};
  for (final seat in hall.seats) {
    final row = seat.rowLabel ?? '?';
    byRow[row] = (byRow[row] ?? 0) + 1;
  }
  return byRow.values.fold(0, (a, b) => a > b ? a : b);
}

const hallScreenTypes = ['Standard', 'IMAX', '3D'];
const hallStatuses = ['Active', 'Maintenance', 'Inactive'];

Color hallStatusColor(int? status) {
  switch (status) {
    case 1:
      return const Color(0xFFF59E0B); // Maintenance — orange
    case 2:
      return const Color(0xFFE50914); // Inactive — red
    default:
      return const Color(0xFF22C55E); // Active — green
  }
}

bool hallIsActive(Hall hall) => hall.status == 0;

String inactiveHallMessage(Hall hall) {
  final statusLabel = hallStatuses[hall.status ?? 2];
  return "Hall '${hall.name ?? 'Unknown'}' is not available ($statusLabel). "
      'Projections can only be scheduled in active halls.';
}
