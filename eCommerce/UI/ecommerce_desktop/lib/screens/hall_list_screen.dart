import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/core/widgets/seat_layout_editor.dart';
import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/providers/hall_provider.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class HallListScreen extends StatefulWidget {
  const HallListScreen({super.key, this.editId, this.onEditConsumed});

  final int? editId;
  final VoidCallback? onEditConsumed;

  @override
  State<HallListScreen> createState() => _HallListScreenState();
}

class _HallListScreenState extends State<HallListScreen> {
  late HallProvider _provider;
  List<Hall> _halls = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<HallProvider>();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final filter = <String, dynamic>{'pageSize': 50};
      if (_searchController.text.isNotEmpty) filter['name'] = _searchController.text;
      final data = await _provider.get(filter: filter);
      if (!mounted) return;
      setState(() {
        _halls = data.items ?? [];
        _loading = false;
      });
      _maybeOpenEdit();
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  void _maybeOpenEdit() {
    final id = widget.editId;
    if (id == null) return;
    Hall? hall;
    for (final h in _halls) {
      if (h.id == id) {
        hall = h;
        break;
      }
    }
    widget.onEditConsumed?.call();
    if (hall != null && mounted) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _showDialog(hall: hall);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage Halls',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search halls...',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 10),
          PrimaryButton(label: 'Add Hall', onPressed: () => _showDialog()),
        ],
      ),
      child: DataCard(
        emptyMessage: _halls.isEmpty ? 'No halls found' : null,
        child: StyledDataTable(
          columns: const [
            DataColumn(label: Text('Hall Name')),
            DataColumn(label: Text('Layout')),
            DataColumn(label: Text('Screen Type')),
            DataColumn(label: Text('Status')),
            DataColumn(label: Text('Actions')),
          ],
          rows: _halls.map(_buildRow).toList(),
        ),
      ),
    );
  }

  DataRow _buildRow(Hall h) {
    return DataRow(cells: [
      DataCell(Row(children: [
        Container(
          width: 32,
          height: 32,
          decoration: BoxDecoration(
            color: AppColors.inputFill,
            borderRadius: BorderRadius.circular(8),
          ),
          child: const Icon(Icons.tv, color: AppColors.textSecondary, size: 16),
        ),
        const SizedBox(width: 10),
        Text(h.name ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
      ])),
      DataCell(Text(hallLayoutLabel(h))),
      DataCell(Text(h.screenTypeName ?? hallScreenTypes[h.screenType ?? 0])),
      DataCell(StatusBadge(
        label: h.statusName ?? hallStatuses[h.status ?? 0],
        color: hallStatusColor(h.status),
        filled: true,
      )),
      DataCell(Row(children: [
        ActionIconButton(
          icon: Icons.event_seat_outlined,
          color: AppColors.green,
          onPressed: () => _showSeatLayoutDialog(h),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          onPressed: () => _showDialog(hall: h),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          onPressed: () => _delete(h),
        ),
      ])),
    ]);
  }

  Future<void> _delete(Hall h) async {
    final ok = await confirmDelete(context, 'Delete "${h.name}"?');
    if (ok != true || !mounted) return;
    try {
      await _provider.remove(h.id!);
      showAppSnackBar(context, 'Hall deleted');
      _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showSeatLayoutDialog(Hall hall) async {
    if (hall.id == null) return;
    try {
      final fullHall = await _provider.getById(hall.id!);
      if (!mounted) return;
      if (fullHall.seats.isEmpty) {
        alertBox(context, 'No seats', 'This hall has no seats yet.');
        return;
      }

      Map<int, int> seatTypes = {
        for (final seat in fullHall.seats)
          if (seat.id != null) seat.id!: seat.normalizedType,
      };
      bool submitting = false;

      await showDialog(
        context: context,
        builder: (dialogContext) => StatefulBuilder(
          builder: (context, setDialogState) => FormDialogShell(
            title: 'Seat layout — ${fullHall.name ?? 'Hall'}',
            submitLabel: 'Save layout',
            isSubmitting: submitting,
            maxWidth: 820,
            onSubmit: () async {
              setDialogState(() => submitting = true);
              final payload = fullHall.seats
                  .where((s) => s.id != null)
                  .map((s) => {
                        'seatId': s.id,
                        'seatType': seatTypes[s.id] ?? 0,
                      })
                  .toList();
              try {
                await _provider.updateSeatLayout(hall.id!, payload);
                if (context.mounted) {
                  Navigator.pop(context);
                  showAppSnackBar(this.context, 'Seat layout saved');
                  _load();
                }
              } on Exception catch (e) {
                setDialogState(() => submitting = false);
                if (context.mounted) alertBox(context, 'Error', e.toString());
              }
            },
            child: SeatLayoutEditor(
              seats: fullHall.seats,
              onChanged: (types) => seatTypes = types,
            ),
          ),
        ),
      );
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showDialog({Hall? hall}) async {
    final nameCtrl = TextEditingController(text: hall?.name ?? '');
    final rowsCtrl = TextEditingController(
      text: hall != null ? '${hall.rowCount ?? derivedRowCount(hall)}' : '5',
    );
    final colsCtrl = TextEditingController(
      text: hall != null ? '${hall.seatsPerRow ?? derivedSeatsPerRow(hall)}' : '8',
    );
    int screenType = hall?.screenType ?? 0;
    int status = hall?.status ?? 0;
    bool submitting = false;
    final isEdit = hall != null;

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: hall == null ? 'Add New Hall' : 'Edit Hall',
          submitLabel: hall == null ? 'Add Hall' : 'Save',
          isSubmitting: submitting,
          onSubmit: () async {
            if (nameCtrl.text.trim().isEmpty) {
              alertBox(context, 'Validation', 'Hall name is required');
              return;
            }
            final rows = int.tryParse(rowsCtrl.text) ?? 0;
            final cols = int.tryParse(colsCtrl.text) ?? 0;
            if (!isEdit && (rows < 1 || cols < 1)) {
              alertBox(context, 'Validation', 'Rows and columns must be at least 1');
              return;
            }
            setDialogState(() => submitting = true);
            final entity = Hall(
              name: nameCtrl.text.trim(),
              screenType: screenType,
              status: status,
              isActive: status == 0,
            );
            try {
              if (hall == null) {
                await _provider.insert(entity.toInsertJson(rowsCount: rows, seatsPerRow: cols));
              } else {
                await _provider.update(hall.id!, entity.toUpdateJson());
              }
              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, hall == null ? 'Hall added' : 'Hall updated');
                _load();
              }
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) alertBox(context, 'Error', e.toString());
            }
          },
          child: Column(
            children: [
              TextField(
                controller: nameCtrl,
                decoration: const InputDecoration(labelText: 'Hall Name', hintText: 'e.g., Hall 1'),
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextField(
                    controller: rowsCtrl,
                    readOnly: isEdit,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: 'Rows',
                      hintText: 'e.g., 5',
                      helperText: isEdit ? 'Use seat layout editor to change couple seats' : null,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: colsCtrl,
                    readOnly: isEdit,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: 'Columns',
                      hintText: 'e.g., 8',
                      helperText: isEdit ? null : 'Seats per row',
                    ),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: screenType,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Screen Type'),
                    items: List.generate(
                      hallScreenTypes.length,
                      (i) => DropdownMenuItem(value: i, child: Text(hallScreenTypes[i])),
                    ),
                    onChanged: (v) => setDialogState(() => screenType = v ?? 0),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: status,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Status'),
                    items: List.generate(
                      hallStatuses.length,
                      (i) => DropdownMenuItem(value: i, child: Text(hallStatuses[i])),
                    ),
                    onChanged: (v) => setDialogState(() => status = v ?? 0),
                  ),
                ),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}
