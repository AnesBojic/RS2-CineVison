import 'package:ecommerce_desktop/models/seat.dart';
import 'package:flutter/material.dart';

class Hall {
  final int? id;
  final String? name;
  final String? description;
  final int? screenTypeId;
  final String? screenTypeName;
  final int? statusId;
  final String? statusName;

  /// Copied from the hall's status: projections may only be scheduled when true.
  final bool? allowsScreenings;
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
    this.screenTypeId,
    this.screenTypeName,
    this.statusId,
    this.statusName,
    this.allowsScreenings,
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
      screenTypeId: json['screenTypeId'] as int?,
      screenTypeName: json['screenTypeName'] as String?,
      statusId: json['statusId'] as int?,
      statusName: json['statusName'] as String?,
      allowsScreenings: json['allowsScreenings'] as bool?,
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
        ...toUpdateJson(),
        'rowsCount': rowsCount,
        'seatsPerRow': seatsPerRow,
      };

  Map<String, dynamic> toUpdateJson() => {
        'name': name,
        'description': description ?? '',
        'screenTypeId': screenTypeId,
        'statusId': statusId,
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

/// Statuses come from the API, so the colour follows the one behaviour that matters:
/// whether the hall can host projections.
Color hallStatusColor(Hall hall) =>
    hall.allowsScreenings == true ? const Color(0xFF22C55E) : const Color(0xFFF59E0B);

bool hallIsActive(Hall hall) => hall.allowsScreenings == true;

String inactiveHallMessage(Hall hall) {
  final statusLabel = hall.statusName ?? 'unavailable';
  return "Hall '${hall.name ?? 'Unknown'}' is not available ($statusLabel). "
      'Projections can only be scheduled in halls whose status allows it.';
}
