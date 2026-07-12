import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/models/seat.dart';
import 'package:flutter/material.dart';

class SeatLayoutEditor extends StatefulWidget {
  const SeatLayoutEditor({
    super.key,
    required this.seats,
    required this.onChanged,
  });

  final List<Seat> seats;
  final ValueChanged<Map<int, int>> onChanged;

  @override
  State<SeatLayoutEditor> createState() => _SeatLayoutEditorState();
}

class _SeatLayoutEditorState extends State<SeatLayoutEditor> {
  late Map<int, int> _types;

  @override
  void initState() {
    super.initState();
    _types = _initialTypes();
  }

  @override
  void didUpdateWidget(covariant SeatLayoutEditor oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.seats != widget.seats) {
      _types = _initialTypes();
    }
  }

  Map<int, int> _initialTypes() {
    return {
      for (final seat in widget.seats)
        if (seat.id != null) seat.id!: seat.normalizedType,
    };
  }

  Set<int> get _partnerSeatIds {
    final partners = <int>{};
    for (final seat in widget.seats) {
      if (seat.id == null) continue;
      for (final other in widget.seats) {
        if (other.id != null && (_types[other.id] ?? 0) == 2) {
          final neighbor = _rightNeighbor(other);
          if (neighbor?.id == seat.id) {
            partners.add(seat.id!);
          }
        }
      }
    }
    return partners;
  }

  bool _isPartnerSlot(Seat seat) {
    if (seat.id == null) return false;
    return _partnerSeatIds.contains(seat.id);
  }

  Seat? _rightNeighbor(Seat seat) {
    final row = widget.seats
        .where((s) => s.rowLabel == seat.rowLabel)
        .toList()
      ..sort((a, b) => (a.seatNumber ?? 0).compareTo(b.seatNumber ?? 0));
    final index = row.indexWhere((s) => s.id == seat.id);
    if (index < 0 || index >= row.length - 1) return null;
    return row[index + 1];
  }

  void _cycleSeat(Seat seat) {
    if (seat.id == null || _isPartnerSlot(seat)) return;
    final current = _types[seat.id] ?? 0;
    final next = current == 2 ? 0 : 2;
    if (next == 2 && _rightNeighbor(seat) == null) return;

    setState(() {
      _types[seat.id!] = next;
    });
    widget.onChanged(_types);
  }

  Color _seatColor(int type) {
    if (type == 2) return AppColors.primary.withValues(alpha: 0.35);
    return AppColors.inputFill;
  }

  Color _seatBorder(int type) {
    if (type == 2) return AppColors.primary;
    return AppColors.cardBorder.withValues(alpha: 0.4);
  }

  @override
  Widget build(BuildContext context) {
    final rows = <String, List<Seat>>{};
    for (final seat in widget.seats) {
      final label = seat.rowLabel ?? '?';
      rows.putIfAbsent(label, () => []).add(seat);
    }
    final sortedRows = rows.keys.toList()..sort();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Seat layout',
          style: TextStyle(
            color: AppColors.textPrimary,
            fontWeight: FontWeight.w600,
            fontSize: 14,
          ),
        ),
        const SizedBox(height: 6),
        const Text(
          'Tap a seat to toggle between Regular and Couple (2 spots). Couple seats use this seat and the one to the right.',
          style: TextStyle(color: AppColors.textSecondary, fontSize: 12),
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 16,
          runSpacing: 8,
          children: [
            _legend(AppColors.inputFill, AppColors.cardBorder, 'Regular'),
            _legend(AppColors.primary.withValues(alpha: 0.35), AppColors.primary, 'Couple'),
          ],
        ),
        const SizedBox(height: 16),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: AppDecorations.card(radius: 12),
          child: Column(
            children: [
              Container(
                height: 8,
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppColors.cardBorder.withValues(alpha: 0.35),
                  borderRadius: BorderRadius.circular(4),
                ),
                alignment: Alignment.center,
                child: const Text('SCREEN', style: TextStyle(color: AppColors.textSecondary, fontSize: 10)),
              ),
              ...sortedRows.map((rowLabel) {
                final rowSeats = rows[rowLabel]!
                  ..sort((a, b) => (a.seatNumber ?? 0).compareTo(b.seatNumber ?? 0));
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Row(
                    children: [
                      SizedBox(
                        width: 24,
                        child: Text(
                          rowLabel,
                          style: const TextStyle(color: AppColors.textSecondary, fontSize: 12),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Wrap(
                          spacing: 6,
                          runSpacing: 6,
                          children: _buildRowWidgets(rowSeats),
                        ),
                      ),
                    ],
                  ),
                );
              }),
            ],
          ),
        ),
      ],
    );
  }

  List<Widget> _buildRowWidgets(List<Seat> rowSeats) {
    final widgets = <Widget>[];
    for (final seat in rowSeats) {
      if (_isPartnerSlot(seat)) continue;
      final type = _types[seat.id] ?? 0;
      final isCouple = type == 2;
      widgets.add(
        GestureDetector(
          onTap: () => _cycleSeat(seat),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            width: isCouple ? 52 : 24,
            height: 24,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: _seatColor(type),
              borderRadius: BorderRadius.circular(6),
              border: Border.all(color: _seatBorder(type)),
            ),
            child: Text(
              isCouple ? '♥' : '${seat.seatNumber}',
              style: TextStyle(
                color: type == 0 ? AppColors.textSecondary : AppColors.textPrimary,
                fontSize: isCouple ? 12 : 10,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ),
      );
    }
    return widgets;
  }

  Widget _legend(Color fill, Color border, String label) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 14,
          height: 14,
          decoration: BoxDecoration(
            color: fill,
            borderRadius: BorderRadius.circular(4),
            border: Border.all(color: border),
          ),
        ),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
      ],
    );
  }
}
