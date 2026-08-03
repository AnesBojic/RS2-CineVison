import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/hall.dart';
import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/models/movie.dart';
import 'package:cinevision_desktop/models/screening.dart';
import 'package:cinevision_desktop/models/search_result.dart';
import 'package:cinevision_desktop/providers/hall_provider.dart';
import 'package:cinevision_desktop/providers/language_provider.dart';
import 'package:cinevision_desktop/providers/movie_provider.dart';
import 'package:cinevision_desktop/providers/screening_provider.dart';
import 'package:cinevision_desktop/utils/api_client_exception.dart';
import 'package:cinevision_desktop/utils/field_validators.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ScreeningListScreen extends StatefulWidget {
  const ScreeningListScreen({super.key, this.editId, this.onEditConsumed});

  final int? editId;
  final VoidCallback? onEditConsumed;

  @override
  State<ScreeningListScreen> createState() => _ScreeningListScreenState();
}

class _ScreeningListScreenState extends State<ScreeningListScreen> {
  late ScreeningProvider _provider;
  List<Screening> _items = [];
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
    _provider = context.read<ScreeningProvider>();
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
      languageProvider.get(filter: {'pageSize': 100, 'isActive': true}),
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
              'status allows screenings.';
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

  String? _moviePoster(Screening s) {
    if (s.moviePosterBase64 != null && s.moviePosterBase64!.isNotEmpty) {
      return s.moviePosterBase64;
    }
    return _movieById(s.movieId)?.posterImageBase64;
  }

  List<Screening> get _filtered {
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
    Screening? screening;
    for (final s in _items) {
      if (s.id == id) {
        screening = s;
        break;
      }
    }
    widget.onEditConsumed?.call();
    if (screening != null && mounted) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _showDialog(screening: screening);
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
                  '${_page}_${_items.map((s) => '${s.id}-${s.isActive}').join('|')}',
                ),
                columns: const [
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Hall')),
                  DataColumn(label: Text('Date')),
                  DataColumn(label: Text('Time')),
                  DataColumn(label: Text('Price')),
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

  DataRow _buildRow(Screening s) {
    return DataRow(cells: [
      DataCell(Row(children: [
        posterThumbnail(_moviePoster(s)),
        const SizedBox(width: 12),
        Text(s.movieTitle ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
      ])),
      DataCell(Text(s.hallName ?? '—')),
      DataCell(Text(formatDate(s.startTime))),
      DataCell(StatusBadge(label: formatTime(s.startTime), color: AppColors.green, filled: true)),
      DataCell(Text(formatCurrency(s.basePrice))),
      actionButtonsCell([
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          tooltip: 'Edit',
          onPressed: () => _showDialog(screening: s),
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

  Future<void> _delete(Screening s) async {
    if (s.id == null) return;

    Map<String, dynamic>? impact;
    try {
      impact = await _provider.getDeleteImpact(s.id!);
    } on Exception catch (_) {}

    if (!mounted) return;
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
      showAppSnackBar(context, 'Projection and related bookings deleted');
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

  Future<void> _showDialog({Screening? screening}) async {
    await _ensurePickerData();
    if (!mounted) return;

    final blockedReason = screening == null ? _newProjectionBlockedReason : null;
    if (blockedReason != null) {
      showAppSnackBar(context, blockedReason, isError: true);
      return;
    }

    int? movieId = screening?.movieId;
    int? hallId = screening?.hallId;
    int? languageId = screening?.languageId;
    final localStart = screening?.startTime?.toLocal();
    DateTime? date = localStart;
    TimeOfDay? time =
        localStart != null ? TimeOfDay.fromDateTime(localStart) : null;
    final priceCtrl = TextEditingController(text: '${screening?.basePrice ?? ''}');
    bool submitting = false;
    final formKey = GlobalKey<FormState>();

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: screening == null ? 'Add New Projection' : 'Edit Projection',
          submitLabel: screening == null ? 'Add Projection' : 'Save',
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
            final entity = Screening(
              movieId: movieId,
              hallId: hallId,
              languageId: languageId,
              startTime: startTime,
              basePrice: num.tryParse(priceCtrl.text.replaceAll('\$', '')) ?? 0,
              isActive: true,
            );
            try {
              if (screening == null) {
                await _provider.insert(entity.toJson());
              } else {
                await _provider.update(screening.id!, entity.toJson());
              }
              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, screening == null ? 'Projection added' : 'Projection updated');
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
                decoration: const InputDecoration(labelText: 'Movie'),
                items: _movies
                    .map((m) => DropdownMenuItem(value: m.id, child: Text(m.title ?? '')))
                    .toList(),
                onChanged: (v) => setDialogState(() => movieId = v),
                validator: (v) => v == null ? 'Movie is required' : null,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                initialValue: hallId,
                dropdownColor: AppColors.card,
                decoration: InputDecoration(
                  labelText: 'Hall',
                  helperText: _halls.any((h) => !hallIsActive(h))
                      ? 'Halls whose status blocks screenings cannot be selected.'
                      : null,
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
                onChanged: (v) => setDialogState(() => hallId = v),
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
                      onTap: () async {
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
                      onTap: () async {
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
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Price', hintText: 'e.g. \$15'),
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
