import 'package:cinevision_desktop/core/enums/api_enums.dart';
import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/hall.dart';
import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/models/movie.dart';
import 'package:cinevision_desktop/models/projection.dart';
import 'package:cinevision_desktop/models/reservation.dart';
import 'package:cinevision_desktop/models/search_result.dart';
import 'package:cinevision_desktop/providers/hall_provider.dart';
import 'package:cinevision_desktop/providers/language_provider.dart';
import 'package:cinevision_desktop/providers/movie_provider.dart';
import 'package:cinevision_desktop/providers/projection_provider.dart';
import 'package:cinevision_desktop/providers/reservation_provider.dart';
import 'package:cinevision_desktop/utils/api_client_exception.dart';
import 'package:cinevision_desktop/utils/field_validators.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ProjectionListScreen extends StatefulWidget {
  const ProjectionListScreen({super.key, this.editId, this.onEditConsumed});

  final int? editId;
  final VoidCallback? onEditConsumed;

  @override
  State<ProjectionListScreen> createState() => _ProjectionListScreenState();
}

class _ProjectionListScreenState extends State<ProjectionListScreen> {
  late ProjectionProvider _provider;
  List<Projection> _items = [];
  List<Movie> _movies = [];
  List<Hall> _halls = [];
  List<LookupItem> _languages = [];
  bool _loading = true;
  static const int _pageSize = 10;
  int _page = 1;
  int _totalCount = 0;
  bool _pickerLoaded = false;
  final _searchController = TextEditingController();

  int get _totalPages =>
      _totalCount == 0 ? 1 : (_totalCount / _pageSize).ceil();

  @override
  void initState() {
    super.initState();
    _provider = context.read<ProjectionProvider>();
    _load();
    // Loaded up front so the toolbar can grey out "Add Projection" with a reason
    // instead of letting the form open and failing afterwards.
    _ensurePickerData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final data = await _provider.get(filter: {
        'page': _page,
        'pageSize': _pageSize,
        'includeTotalCount': true,
        'includeSeatStats': false,
        'includePoster': true,
      });

      if (!mounted) return;
      setState(() {
        _items = data.items ?? [];
        _totalCount = data.totalCount ?? _items.length;
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

  Future<void> _ensurePickerData() async {
    // Keyed on the fetch having happened rather than on the lists being non-empty,
    // otherwise a genuinely empty lookup would be re-fetched on every dialog open.
    if (_pickerLoaded) return;

    final movieProvider = context.read<MovieProvider>();
    final hallProvider = context.read<HallProvider>();
    final languageProvider = context.read<LanguageProvider>();

    final results = await Future.wait([
      movieProvider.get(filter: {'pageSize': 500}),
      hallProvider.get(filter: {'pageSize': 500}),
      languageProvider.get(filter: {'pageSize': 100}),
    ]);

    if (!mounted) return;
    setState(() {
      _movies = (results[0] as SearchResult<Movie>).items ?? [];
      _halls = (results[1] as SearchResult<Hall>).items ?? [];
      _languages = (results[2] as SearchResult<LookupItem>).items ?? [];
      _pickerLoaded = true;
    });
  }

  /// Why a new projection cannot be started right now, or null when it can.
  /// Editing an existing one stays possible either way.
  String? get _newProjectionBlockedReason {
    if (!_pickerLoaded) return null;
    if (_movies.isEmpty) {
      return 'Add at least one movie before creating a projection.';
    }
    if (!_halls.any(hallIsActive)) {
      return _halls.isEmpty
          ? 'Add at least one hall before creating a projection.'
          : 'Every hall is currently unavailable. A projection needs a hall whose '
              'status allows projections.';
    }
    return null;
  }

  Hall? _hallById(int? id) {
    if (id == null) return null;
    for (final hall in _halls) {
      if (hall.id == id) return hall;
    }
    return null;
  }

  Movie? _movieById(int? id) {
    if (id == null) return null;
    for (final movie in _movies) {
      if (movie.id == id) return movie;
    }
    return null;
  }

  String? _moviePoster(Projection s) {
    if (s.moviePosterBase64 != null && s.moviePosterBase64!.isNotEmpty) {
      return s.moviePosterBase64;
    }
    return _movieById(s.movieId)?.posterImageBase64;
  }

  List<Projection> get _filtered {
    final q = _searchController.text.toLowerCase();
    if (q.isEmpty) return _items;
    return _items.where((s) {
      return (s.movieTitle ?? '').toLowerCase().contains(q) ||
          (s.hallName ?? '').toLowerCase().contains(q);
    }).toList();
  }

  void _maybeOpenEdit() {
    final id = widget.editId;
    if (id == null) return;
    Projection? projection;
    for (final s in _items) {
      if (s.id == id) {
        projection = s;
        break;
      }
    }
    widget.onEditConsumed?.call();
    if (projection != null && mounted) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _showDialog(projection: projection);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage Projections',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search projections...',
            onSubmitted: (_) => setState(() {}),
          ),
          const SizedBox(width: 10),
          PrimaryButton(
            label: 'Add Projection',
            onPressed:
                _newProjectionBlockedReason == null ? () => _showDialog() : null,
            tooltip: _newProjectionBlockedReason,
          ),
        ],
      ),
      child: Column(
        children: [
          Expanded(
            child: DataCard(
              emptyMessage:
                  _filtered.isEmpty ? 'No projections found' : null,
              child: StyledDataTable(
                key: ValueKey(
                  '${_page}_${_items.map((s) => '${s.id}').join('|')}',
                ),
                columns: const [
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Hall')),
                  DataColumn(label: Text('Date')),
                  DataColumn(label: Text('Time')),
                  DataColumn(label: Text('Price')),
                  DataColumn(label: Text('Cancellation reason')),
                  actionsDataColumn,
                ],
                rows: _filtered.map(_buildRow).toList(),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'Page $_page of $_totalPages · $_totalCount projections',
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(width: 12),
              IconButton(
                tooltip: 'Previous page',
                onPressed: _page > 1 && !_loading
                    ? () {
                        setState(() => _page--);
                        _load();
                      }
                    : null,
                icon: const Icon(Icons.chevron_left),
              ),
              IconButton(
                tooltip: 'Next page',
                onPressed: _page < _totalPages && !_loading
                    ? () {
                        setState(() => _page++);
                        _load();
                      }
                    : null,
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
        ],
      ),
    );
  }

  DataRow _buildRow(Projection s) {
    return DataRow(cells: [
      DataCell(Row(children: [
        posterThumbnail(_moviePoster(s)),
        const SizedBox(width: 12),
        Text(s.movieTitle ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
      ])),
      DataCell(Text(s.hallName ?? '—')),
      DataCell(Text(formatDate(s.startTime))),
      DataCell(s.isCancelled
          ? const StatusBadge(label: 'Cancelled', color: AppColors.orange, filled: true)
          : StatusBadge(label: formatTime(s.startTime), color: AppColors.green, filled: true)),
      DataCell(Text(formatCurrency(s.basePrice))),
      DataCell(
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 260),
          child: Text(
            s.isCancelled
                ? (s.cancellationReason?.trim().isNotEmpty == true
                    ? s.cancellationReason!
                    : 'No reason recorded')
                : '—',
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ),
      actionButtonsCell([
        if (s.isCancelled)
          ActionIconButton(
            icon: Icons.info_outline,
            color: AppColors.blue,
            tooltip: 'Cancellation details',
            onPressed: () => _showCancellationDetails(s),
          ),
        if (!s.isCancelled)
          ActionIconButton(
            icon: Icons.edit_outlined,
            color: AppColors.blue,
            tooltip: s.hasBookings ? 'Edit language only' : 'Edit',
            onPressed: () => _showDialog(projection: s),
          ),
        if (!s.isCancelled && s.isUpcoming)
          ActionIconButton(
            icon: Icons.event_busy,
            color: AppColors.orange,
            tooltip: 'Cancel projection',
            onPressed: () => _cancel(s),
          ),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          tooltip: 'Delete',
          onPressed: () => _delete(s),
        ),
      ]),
    ]);
  }

  Future<void> _showCancellationDetails(Projection s) async {
    List<Reservation> tickets = [];
    try {
      final data = await context.read<ReservationProvider>().get(filter: {
        'page': 1,
        'pageSize': 100,
        'includeTotalCount': false,
        'projectionId': s.id,
        'status': ReservationStatus.cancelled,
      });
      tickets = data.items ?? [];
    } on Exception catch (_) {}

    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Text(
          'Cancelled · ${s.movieTitle ?? 'Projection'}',
          style: const TextStyle(color: AppColors.textPrimary),
        ),
        content: SizedBox(
          width: 520,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Projection reason',
                style: TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                s.cancellationReason?.trim().isNotEmpty == true
                    ? s.cancellationReason!
                    : 'No reason recorded',
                style: const TextStyle(color: AppColors.textPrimary),
              ),
              const SizedBox(height: 16),
              const Text(
                'Ticket reasons',
                style: TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 8),
              if (tickets.isEmpty)
                const Text(
                  'No cancelled tickets for this projection.',
                  style: TextStyle(color: AppColors.textSecondary),
                )
              else
                ConstrainedBox(
                  constraints: const BoxConstraints(maxHeight: 280),
                  child: ListView.separated(
                    shrinkWrap: true,
                    itemCount: tickets.length,
                    separatorBuilder: (context, index) => const Divider(height: 16),
                    itemBuilder: (context, index) {
                      final t = tickets[index];
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            '${t.reservationNumber} · ${t.customerName ?? t.customerEmail ?? 'Customer'}',
                            style: const TextStyle(
                              color: AppColors.textPrimary,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            t.reasonLabel,
                            style: const TextStyle(color: AppColors.textSecondary),
                          ),
                        ],
                      );
                    },
                  ),
                ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  Future<void> _delete(Projection s) async {
    if (s.id == null) return;

    Map<String, dynamic>? impact;
    try {
      impact = await _provider.getDeleteImpact(s.id!);
    } on Exception catch (_) {}

    if (!mounted) return;
    final blocked = cascadeDeleteBlockReason(impact);
    if (blocked != null) {
      if (s.isCancelled) {
        alertBox(context, 'Cannot delete', blocked);
        return;
      }
      if (s.isUpcoming) {
        await _cancel(
          s,
          extraMessage:
              'This projection has bookings, so it cannot be deleted. Cancel it instead: customers are refunded and the sold tickets stay on record.',
        );
        return;
      }
      alertBox(
        context,
        'Cannot delete',
        '$blocked A projection that has already started cannot be cancelled either.',
      );
      return;
    }

    final label = s.movieTitle?.isNotEmpty == true
        ? 'projection "${s.movieTitle}"'
        : 'this projection';
    final ok = await confirmDelete(
      context,
      buildCascadeDeleteWarning(subjectLabel: label, impact: impact),
    );
    if (ok != true || !mounted) return;
    try {
      await _provider.remove(s.id!);
      if (!mounted) return;
      showAppSnackBar(context, 'Projection deleted');
      // Optimistically remove so UI updates even before reload finishes.
      setState(() {
        _items = _items.where((x) => x.id != s.id).toList();
        _totalCount = (_totalCount - 1).clamp(0, 1 << 30);
        if (_items.isEmpty && _page > 1) {
          _page--;
        }
      });
      await _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _cancel(Projection s, {String? extraMessage}) async {
    if (s.id == null) return;
    final title = s.movieTitle?.isNotEmpty == true ? '"${s.movieTitle}"' : 'this projection';
    final ok = await confirmCancel(
      context,
      extraMessage ??
          'Cancel $title?\n\nActive bookings will be cancelled and paid tickets refunded. The projection stays on record with its original movie, hall and time.',
    );
    if (ok != true || !mounted) return;
    try {
      await _provider.cancel(s.id!);
      if (!mounted) return;
      showAppSnackBar(context, 'Projection cancelled');
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot cancel', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showDialog({Projection? projection}) async {
    await _ensurePickerData();
    if (!mounted) return;

    final blockedReason = projection == null ? _newProjectionBlockedReason : null;
    if (blockedReason != null) {
      showAppSnackBar(context, blockedReason, isError: true);
      return;
    }

    if (projection?.isCancelled == true) {
      showAppSnackBar(context, 'Cancelled projections cannot be edited.', isError: true);
      return;
    }

    final scheduleLocked = projection?.hasBookings == true;
    const soldLockHint = 'Sold tickets freeze movie, hall, time and price.';

    int? movieId = projection?.movieId;
    int? hallId = projection?.hallId;
    int? languageId = projection?.languageId;
    final localStart = projection?.startTime?.toLocal();
    DateTime? date = localStart;
    TimeOfDay? time =
        localStart != null ? TimeOfDay.fromDateTime(localStart) : null;
    final priceCtrl = TextEditingController(text: '${projection?.basePrice ?? ''}');
    bool submitting = false;
    final formKey = GlobalKey<FormState>();

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: projection == null ? 'Add New Projection' : 'Edit Projection',
          submitLabel: projection == null ? 'Add Projection' : 'Save',
          isSubmitting: submitting,
          onSubmit: () async {
            if (!(formKey.currentState?.validate() ?? false)) return;
            setDialogState(() => submitting = true);
            final selectedDate = date!;
            final selectedTime = time!;
            final startTime = DateTime(
              selectedDate.year,
              selectedDate.month,
              selectedDate.day,
              selectedTime.hour,
              selectedTime.minute,
            );
            final entity = Projection(
              movieId: movieId,
              hallId: hallId,
              languageId: languageId,
              startTime: startTime,
              basePrice: num.tryParse(priceCtrl.text.replaceAll('\$', '')) ?? 0,
            );
            try {
              if (projection == null) {
                await _provider.insert(entity.toJson());
              } else {
                await _provider.update(projection.id!, entity.toJson());
              }
              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, projection == null ? 'Projection added' : 'Projection updated');
                await _load();
              }
            } on ApiClientException catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) {
                showAppSnackBar(context, e.message, isError: true);
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
              DropdownButtonFormField<int>(
                initialValue: movieId,
                dropdownColor: AppColors.card,
                decoration: InputDecoration(
                  labelText: 'Movie',
                  helperText: scheduleLocked ? soldLockHint : null,
                ),
                items: _movies
                    .map((m) => DropdownMenuItem(value: m.id, child: Text(m.title ?? '')))
                    .toList(),
                onChanged: scheduleLocked ? null : (v) => setDialogState(() => movieId = v),
                validator: (v) => v == null ? 'Movie is required' : null,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                initialValue: hallId,
                dropdownColor: AppColors.card,
                decoration: InputDecoration(
                  labelText: 'Hall',
                  helperText: scheduleLocked
                      ? soldLockHint
                      : (_halls.any((h) => !hallIsActive(h))
                          ? 'Halls whose status blocks projections cannot be selected.'
                          : null),
                ),
                // Unavailable halls stay visible but greyed out, with the status
                // spelled out, rather than being selectable and rejected afterwards.
                items: _halls.map((h) {
                  final available = hallIsActive(h);
                  return DropdownMenuItem(
                    value: h.id,
                    enabled: available,
                    child: Text(
                      available
                          ? (h.name ?? '')
                          : '${h.name ?? ''} — ${h.statusName ?? 'unavailable'}',
                      style: available
                          ? null
                          : const TextStyle(color: AppColors.textSecondary),
                    ),
                  );
                }).toList(),
                onChanged: scheduleLocked ? null : (v) => setDialogState(() => hallId = v),
                // Still validated: an existing projection may point at a hall that
                // was taken out of service after it was scheduled.
                autovalidateMode: AutovalidateMode.onUserInteraction,
                validator: (v) {
                  if (v == null) return 'Hall is required';
                  final hall = _hallById(v);
                  if (hall != null && !hallIsActive(hall)) {
                    return inactiveHallMessage(hall);
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  // Wrapped in a FormField so a missing date reports under the field,
                  // the same way the text inputs do.
                  child: FormField<DateTime>(
                    validator: (_) => date == null ? 'Date is required' : null,
                    builder: (fieldState) => InkWell(
                      onTap: scheduleLocked
                          ? null
                          : () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: date ?? DateTime.now(),
                          firstDate: DateTime.now(),
                          lastDate: DateTime(2100),
                        );
                        if (picked == null) return;
                        setDialogState(() => date = picked);
                        fieldState.didChange(picked);
                      },
                      child: InputDecorator(
                        decoration: InputDecoration(
                          labelText: 'Date',
                          errorText: fieldState.errorText,
                        ),
                        child: Text(formatDate(date)),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FormField<TimeOfDay>(
                    validator: (_) => time == null ? 'Time is required' : null,
                    builder: (fieldState) => InkWell(
                      onTap: scheduleLocked
                          ? null
                          : () async {
                        final picked = await showTimePicker(
                          context: context,
                          initialTime: time ?? TimeOfDay.now(),
                        );
                        if (picked == null) return;
                        setDialogState(() => time = picked);
                        fieldState.didChange(picked);
                      },
                      child: InputDecorator(
                        decoration: InputDecoration(
                          labelText: 'Time',
                          errorText: fieldState.errorText,
                        ),
                        child: Text(time != null ? time!.format(context) : '—'),
                      ),
                    ),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                initialValue: languageId,
                dropdownColor: AppColors.card,
                decoration: const InputDecoration(labelText: 'Language'),
                items: _languages
                    .map((l) => DropdownMenuItem(value: l.id, child: Text(l.name ?? '')))
                    .toList(),
                onChanged: (v) => setDialogState(() => languageId = v),
                validator: (v) => v == null ? 'Language is required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: priceCtrl,
                readOnly: scheduleLocked,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(
                  labelText: 'Price',
                  hintText: 'e.g. \$15',
                  helperText: scheduleLocked ? soldLockHint : null,
                ),
                validator: FieldValidators.price,
              ),
            ],
            ),
          ),
        ),
      ),
    );
  }
}
