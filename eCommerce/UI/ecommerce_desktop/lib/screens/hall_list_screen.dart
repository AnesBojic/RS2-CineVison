import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/core/widgets/seat_layout_editor.dart';
import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/hall_provider.dart';
import 'package:ecommerce_desktop/providers/hall_status_provider.dart';
import 'package:ecommerce_desktop/providers/screen_type_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/field_validators.dart';
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
  late ScreenTypeProvider _screenTypeProvider;
  late HallStatusProvider _hallStatusProvider;
  List<Hall> _halls = [];
  List<LookupItem> _screenTypes = [];
  List<LookupItem> _hallStatuses = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<HallProvider>();
    _screenTypeProvider = context.read<ScreenTypeProvider>();
    _hallStatusProvider = context.read<HallStatusProvider>();
    _load();
  }

  Future<void> _loadLookups() async {
    const filter = {'pageSize': 100, 'isActive': true};
    final results = await Future.wait([
      _screenTypeProvider.get(filter: filter),
      _hallStatusProvider.get(filter: filter),
    ]);
    if (!mounted) return;
    setState(() {
      _screenTypes = results[0].items ?? [];
      _hallStatuses = results[1].items ?? [];
    });
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
      await _loadLookups();
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
          PrimaryButton(
            label: 'Add Hall',
            onPressed:
                _missingReferenceDataReason == null ? () => _showDialog() : null,
            tooltip: _missingReferenceDataReason,
          ),
        ],
      ),
      child: DataCard(
        emptyMessage: _halls.isEmpty ? 'No halls found' : null,
        child: StyledDataTable(
          key: ValueKey(_halls.map((h) => '${h.id}-${h.statusId}').join('|')),
          columns: const [
            DataColumn(label: Text('Hall Name')),
            DataColumn(label: Text('Layout')),
            DataColumn(label: Text('Screen Type')),
            DataColumn(label: Text('Status')),
            actionsDataColumn,
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
      DataCell(Text(h.screenTypeName ?? '—')),
      DataCell(StatusBadge(
        label: h.statusName ?? '—',
        color: hallStatusColor(h),
        filled: true,
      )),
      actionButtonsCell([
        ActionIconButton(
          icon: Icons.event_seat_outlined,
          color: AppColors.green,
          tooltip: 'Seats',
          onPressed: () => _showSeatLayoutDialog(h),
        ),
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          tooltip: 'Edit',
          onPressed: () => _showDialog(hall: h),
        ),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          tooltip: 'Delete',
          onPressed: () => _delete(h),
        ),
      ]),
    ]);
  }

  Future<void> _delete(Hall h) async {
    if (h.id == null) return;

    Map<String, dynamic>? impact;
    try {
      impact = await _provider.getDeleteImpact(h.id!);
    } on Exception catch (_) {}

    if (!mounted) return;
    final ok = await confirmDelete(
      context,
      buildCascadeDeleteWarning(
        subjectLabel: '"${h.name}"',
        impact: impact,
      ),
    );
    if (ok != true || !mounted) return;
    try {
      await _provider.remove(h.id!);
      showAppSnackBar(context, 'Hall and related data deleted');
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) showAppSnackBar(context, e.message, isError: true);
    } on Exception catch (e) {
      if (mounted) showAppSnackBar(context, e.toString(), isError: true);
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
                  await _load();
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

  /// Null when screen type and status lookups are ready for create/edit.
  String? get _missingReferenceDataReason =>
      _loading || (_screenTypes.isNotEmpty && _hallStatuses.isNotEmpty)
          ? null
          : 'Add at least one screen type and one hall status under Reference Data '
              'before creating or editing a hall.';

  Future<void> _showDialog({Hall? hall}) async {
    final blockedReason = _missingReferenceDataReason;
    if (blockedReason != null) {
      showAppSnackBar(context, blockedReason, isError: true);
      return;
    }

    final nameCtrl = TextEditingController(text: hall?.name ?? '');
    final rowsCtrl = TextEditingController(
      text: hall != null ? '${hall.rowCount ?? derivedRowCount(hall)}' : '5',
    );
    final colsCtrl = TextEditingController(
      text: hall != null ? '${hall.seatsPerRow ?? derivedSeatsPerRow(hall)}' : '8',
    );
    int screenTypeId = hall?.screenTypeId ?? _screenTypes.first.id!;
    int statusId = hall?.statusId ?? _hallStatuses.first.id!;
    bool submitting = false;
    final isEdit = hall != null;
    final formKey = GlobalKey<FormState>();

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: hall == null ? 'Add New Hall' : 'Edit Hall',
          submitLabel: hall == null ? 'Add Hall' : 'Save',
          isSubmitting: submitting,
          onSubmit: () async {
            if (!(formKey.currentState?.validate() ?? false)) return;
            final rows = int.tryParse(rowsCtrl.text.trim()) ?? 0;
            final cols = int.tryParse(colsCtrl.text.trim()) ?? 0;
            setDialogState(() => submitting = true);
            final entity = Hall(
              name: nameCtrl.text.trim(),
              screenTypeId: screenTypeId,
              statusId: statusId,
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
                await _load();
              }
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) alertBox(context, 'Error', e.toString());
            }
          },
          child: Form(
            key: formKey,
            child: Column(
            children: [
              TextFormField(
                controller: nameCtrl,
                decoration: const InputDecoration(labelText: 'Hall Name', hintText: 'e.g., Hall 1'),
                validator: (v) => FieldValidators.required(v, field: 'Hall name'),
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextFormField(
                    controller: rowsCtrl,
                    readOnly: isEdit,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: 'Rows',
                      hintText: 'e.g., 5',
                      helperText: isEdit ? 'Use seat layout editor to change couple seats' : null,
                    ),
                    // Rows are fixed once seats exist, so only a new hall needs the check.
                    validator: isEdit
                        ? null
                        : (v) => FieldValidators.integer(v, field: 'Rows', max: 50),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: colsCtrl,
                    readOnly: isEdit,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: 'Columns',
                      hintText: 'e.g., 8',
                      helperText: isEdit ? null : 'Seats per row',
                    ),
                    validator: isEdit
                        ? null
                        : (v) => FieldValidators.integer(v, field: 'Columns', max: 50),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: screenTypeId,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Screen Type'),
                    items: _screenTypes
                        .map((t) => DropdownMenuItem(value: t.id, child: Text(t.name ?? '')))
                        .toList(),
                    onChanged: (v) =>
                        setDialogState(() => screenTypeId = v ?? screenTypeId),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: statusId,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Status'),
                    items: _hallStatuses
                        .map((s) => DropdownMenuItem(value: s.id, child: Text(s.name ?? '')))
                        .toList(),
                    onChanged: (v) => setDialogState(() => statusId = v ?? statusId),
                  ),
                ),
              ]),
            ],
          ),
          ),
        ),
      ),
    );
  }
}
