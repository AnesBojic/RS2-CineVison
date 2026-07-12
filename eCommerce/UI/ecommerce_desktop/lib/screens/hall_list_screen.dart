import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
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
            DataColumn(label: Text('Capacity')),
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
      DataCell(Text('${h.capacity ?? h.seatCount ?? 0} seats')),
      DataCell(Text(h.screenTypeName ?? hallScreenTypes[h.screenType ?? 0])),
      DataCell(StatusBadge(
        label: h.statusName ?? hallStatuses[h.status ?? 0],
        color: hallStatusColor(h.status),
        filled: true,
      )),
      DataCell(Row(children: [
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

  Future<void> _showDialog({Hall? hall}) async {
    final nameCtrl = TextEditingController(text: hall?.name ?? '');
    final capacityCtrl = TextEditingController(
      text: hall != null ? '${hall.capacity ?? hall.seatCount ?? 0}' : '',
    );
    int screenType = hall?.screenType ?? 0;
    int status = hall?.status ?? 0;
    bool submitting = false;

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
            setDialogState(() => submitting = true);
            final capacity = int.tryParse(capacityCtrl.text) ?? 0;
            final rows = capacity > 0 ? (capacity / 10).ceil() : 10;
            final seatsPerRow = capacity > 0 ? (capacity / rows).ceil() : 10;
            final entity = Hall(
              name: nameCtrl.text.trim(),
              screenType: screenType,
              status: status,
              isActive: status == 0,
            );
            try {
              if (hall == null) {
                await _provider.insert(entity.toInsertJson(rowsCount: rows, seatsPerRow: seatsPerRow));
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
              Row(children: [
                Expanded(
                  child: TextField(
                    controller: nameCtrl,
                    decoration: const InputDecoration(labelText: 'Hall Name', hintText: 'e.g., Hall 1'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: capacityCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Capacity', hintText: 'Enter seat capacity'),
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
