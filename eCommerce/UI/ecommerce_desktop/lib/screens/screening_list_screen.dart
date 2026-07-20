import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/models/movie.dart';
import 'package:ecommerce_desktop/models/screening.dart';
import 'package:ecommerce_desktop/models/search_result.dart';
import 'package:ecommerce_desktop/providers/hall_provider.dart';
import 'package:ecommerce_desktop/providers/movie_provider.dart';
import 'package:ecommerce_desktop/providers/screening_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
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
  bool _loading = true;
  static const int _pageSize = 10;
  int _page = 1;
  int _totalCount = 0;
  final _searchController = TextEditingController();

  int get _totalPages =>
      _totalCount == 0 ? 1 : (_totalCount / _pageSize).ceil();

  @override
  void initState() {
    super.initState();
    _provider = context.read<ScreeningProvider>();
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
      final data = await _provider.get(filter: {
        'page': _page,
        'pageSize': _pageSize,
        'includeTotalCount': true,
        'includeSeatStats': false,
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
    if (_movies.isNotEmpty && _halls.isNotEmpty) return;

    final movieProvider = context.read<MovieProvider>();
    final hallProvider = context.read<HallProvider>();

    final results = await Future.wait([
      movieProvider.get(filter: {'pageSize': 500}),
      hallProvider.get(filter: {'pageSize': 500}),
    ]);

    if (!mounted) return;
    setState(() {
      _movies = (results[0] as SearchResult<Movie>).items ?? [];
      _halls = (results[1] as SearchResult<Hall>).items ?? [];
    });
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
          PrimaryButton(label: 'Add Projection', onPressed: () => _showDialog()),
        ],
      ),
      child: Column(
        children: [
          Expanded(
            child: DataCard(
              emptyMessage:
                  _filtered.isEmpty ? 'No projections found' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Hall')),
                  DataColumn(label: Text('Date')),
                  DataColumn(label: Text('Time')),
                  DataColumn(label: Text('Price')),
                  DataColumn(label: Text('Actions')),
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
      DataCell(Row(children: [
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          onPressed: () => _showDialog(screening: s),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          onPressed: () => _delete(s),
        ),
      ])),
    ]);
  }

  Future<void> _delete(Screening s) async {
    final ok = await confirmDelete(context, 'Delete this projection?');
    if (ok != true || !mounted) return;
    try {
      await _provider.remove(s.id!);
      showAppSnackBar(context, 'Projection deleted');
      if (_items.length == 1 && _page > 1) {
        _page--;
      }
      _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showDialog({Screening? screening}) async {
    await _ensurePickerData();
    if (!mounted) return;

    int? movieId = screening?.movieId;
    int? hallId = screening?.hallId;
    DateTime? date = screening?.startTime;
    TimeOfDay? time = screening?.startTime != null
        ? TimeOfDay.fromDateTime(screening!.startTime!.toLocal())
        : null;
    final priceCtrl = TextEditingController(text: '${screening?.basePrice ?? ''}');
    bool submitting = false;

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: screening == null ? 'Add New Projection' : 'Edit Projection',
          submitLabel: screening == null ? 'Add Projection' : 'Save',
          isSubmitting: submitting,
          onSubmit: () async {
            if (movieId == null || hallId == null || date == null || time == null) {
              alertBox(context, 'Validation', 'Please fill all fields');
              return;
            }
            final selectedHall = _hallById(hallId);
            if (selectedHall != null && !hallIsActive(selectedHall)) {
              showAppSnackBar(
                context,
                inactiveHallMessage(selectedHall),
                isError: true,
              );
              return;
            }

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
                _load();
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
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                initialValue: hallId,
                dropdownColor: AppColors.card,
                decoration: const InputDecoration(labelText: 'Hall'),
                items: _halls
                    .map((h) => DropdownMenuItem(
                          value: h.id,
                          child: Text(
                            '${h.name ?? ''} (${hallStatuses[h.status ?? 0]})',
                          ),
                        ))
                    .toList(),
                onChanged: (v) {
                  setDialogState(() => hallId = v);
                  if (v == null) return;
                  final hall = _hallById(v);
                  if (hall != null && !hallIsActive(hall) && context.mounted) {
                    showAppSnackBar(
                      context,
                      inactiveHallMessage(hall),
                      isError: true,
                    );
                  }
                },
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: context,
                        initialDate: date ?? DateTime.now(),
                        firstDate: DateTime.now(),
                        lastDate: DateTime(2100),
                      );
                      if (picked != null) setDialogState(() => date = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Date'),
                      child: Text(formatDate(date)),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showTimePicker(
                        context: context,
                        initialTime: time ?? TimeOfDay.now(),
                      );
                      if (picked != null) setDialogState(() => time = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Time'),
                      child: Text(time != null ? time!.format(context) : '—'),
                    ),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              TextField(
                controller: priceCtrl,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Price', hintText: 'e.g. \$15'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
