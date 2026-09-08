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
  const ProjectionListScreen({
    super.key,
    this.editId,
    this.onEditConsumed,
    this.onNavigate,
  });

  final int? editId;
  final VoidCallback? onEditConsumed;
  final void Function(int index)? onNavigate;

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
  String? _statusFilter = 'upcoming';
  String? _hallFilter = 'active';

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
      final filter = <String, dynamic>{
        'page': _page,
        'pageSize': _pageSize,
        'includeTotalCount': true,
        'includeSeatStats': false,
        'includePoster': true,
      };
      if (_statusFilter != null) {
        filter['status'] = _statusFilter;
      }
      if (_hallFilter == 'active') {
        filter['activeHallsOnly'] = true;
      } else if (_hallFilter != null) {
        filter['hallId'] = int.parse(_hallFilter!);
      }
      final data = await _provider.get(filter: filter);

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

  /// Active halls first so the default filter and the add-form picker stay consistent.
  Iterable<Hall> get _hallsForFilter {
    final withId = _halls.where((h) => h.id != null).toList();
    withId.sort((a, b) {
      final activeCmp = (hallIsActive(b) ? 1 : 0) - (hallIsActive(a) ? 1 : 0);
      if (activeCmp != 0) return activeCmp;
      return (a.name ?? '').compareTo(b.name ?? '');
    });
    return withId;
  }

  int? get _defaultActiveHallId {
    for (final hall in _hallsForFilter) {
      if (hallIsActive(hall)) return hall.id;
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
        if (mounted) _edit(projection!);
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
          FilterDropdown(
            hint: 'All statuses',
            value: _statusFilter,
            items: const [
              DropdownMenuItem(value: null, child: Text('All statuses')),
              DropdownMenuItem(value: 'upcoming', child: Text('Active')),
              DropdownMenuItem(value: 'past', child: Text('Past')),
              DropdownMenuItem(value: 'cancelled', child: Text('Cancelled')),
            ],
            onChanged: (v) {
              setState(() {
                _statusFilter = v;
                _page = 1;
              });
              _load();
            },
          ),
          const SizedBox(width: 10),
          FilterDropdown(
            hint: 'Active halls',
            value: _hallFilter,
            items: [
              const DropdownMenuItem(value: 'active', child: Text('Active halls')),
              const DropdownMenuItem(value: null, child: Text('All halls')),
              ..._hallsForFilter.map((h) {
                final name = (h.name ?? '').trim();
                final label = name.isEmpty ? 'Hall ${h.id}' : name;
                return DropdownMenuItem(
                  value: '${h.id}',
                  child: Text(
                    hallIsActive(h) ? label : '$label — ${h.statusName ?? 'unavailable'}',
                  ),
                );
              }),
            ],
            onChanged: (v) {
              setState(() {
                _hallFilter = v;
                _page = 1;
              });
              _load();
            },
          ),
          const SizedBox(width: 10),
          SearchField(
            controller: _searchController,
            hint: 'Search projections...',
            width: 220,
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
                  DataColumn(label: Text('Status')),
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

  Widget _statusBadge(Projection s) {
    if (s.isCancelled) {
      return const StatusBadge(label: 'Cancelled', color: AppColors.orange, filled: true);
    }
    if (s.isUpcoming) {
      return const StatusBadge(label: 'Active', color: AppColors.green, filled: true);
    }
    return const StatusBadge(label: 'Past', color: AppColors.textSecondary, filled: true);
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
      DataCell(Text(formatTime(s.startTime))),
      DataCell(_statusBadge(s)),
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
        ActionIconButton(
          icon: Icons.info_outline,
          color: AppColors.blue,
          tooltip: 'Info',
          onPressed: () => _showProjectionInfo(s),
        ),
        if (!s.isCancelled)
          ActionIconButton(
            icon: Icons.edit_outlined,
            color: AppColors.blue,
            tooltip: s.hasBookings
                ? 'Cannot edit — has booking history'
                : 'Edit',
            onPressed: () => _edit(s),
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
          tooltip: s.hasBookings ? 'Cannot delete — has booking history' : 'Delete',
          onPressed: () => _delete(s),
        ),
      ]),
    ]);
  }

  Future<List<Reservation>> _ticketsForProjection(int? projectionId) async {
    if (projectionId == null) return const [];
    try {
      final data = await context.read<ReservationProvider>().get(filter: {
        'page': 1,
        'pageSize': 100,
        'includeTotalCount': false,
        'projectionId': projectionId,
      });
      return data.items ?? const [];
    } on Exception catch (_) {
      return const [];
    }
  }

  Future<void> _showProjectionInfo(Projection s) {
    final ticketsFuture = _ticketsForProjection(s.id);
    return showDialog<void>(
      context: context,
      builder: (context) => FutureBuilder<List<Reservation>>(
        future: ticketsFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return AlertDialog(
              backgroundColor: AppColors.card,
              content: const SizedBox(
                width: 360,
                height: 88,
                child: Center(
                  child: CircularProgressIndicator(color: AppColors.blue),
                ),
              ),
            );
          }

          final tickets = snapshot.data ?? const <Reservation>[];
          final hasHistory = s.hasBookings || tickets.isNotEmpty;
          final statusLabel = s.isCancelled
              ? 'Cancelled'
              : s.isUpcoming
                  ? 'Active'
                  : 'Past';
          final statusColor = s.isCancelled
              ? AppColors.orange
              : s.isUpcoming
                  ? AppColors.green
                  : AppColors.textSecondary;
          final canEdit = !s.isCancelled && !hasHistory;
          final canCancelShow = s.isUpcoming && !s.isCancelled;
          final canDelete = !hasHistory;
          final failedRefunds = tickets
              .where((t) => t.refundStatus == RefundStatus.failed)
              .length;
          final language = s.language?.trim();
          final reason = s.cancellationReason?.trim();

          return AlertDialog(
            backgroundColor: AppColors.card,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            title: Row(
              children: [
                Expanded(
                  child: Text(
                    s.movieTitle?.trim().isNotEmpty == true
                        ? s.movieTitle!
                        : 'Projection',
                    style: const TextStyle(color: AppColors.textPrimary),
                  ),
                ),
                StatusBadge(label: statusLabel, color: statusColor, filled: true),
              ],
            ),
            content: SizedBox(
              width: 460,
              child: SingleChildScrollView(
                child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _infoLine('Hall', s.hallName ?? '—'),
                  _infoLine(
                    'Show',
                    '${formatDate(s.startTime)}  ${formatTime(s.startTime)}',
                  ),
                  _infoLine('Price', formatCurrency(s.basePrice)),
                  _infoLine(
                    'Language',
                    (language == null || language.isEmpty) ? '—' : language,
                  ),
                  _infoLine('Bookings', _bookingSummary(s, tickets)),
                  if (s.isCancelled)
                    _infoLine(
                      'Reason',
                      (reason == null || reason.isEmpty)
                          ? 'No reason recorded'
                          : reason,
                    ),
                  if (failedRefunds > 0) ...[
                    const SizedBox(height: 8),
                    Text(
                      '$failedRefunds Stripe refund${failedRefunds == 1 ? '' : 's'} failed. Open Bookings and use Refund on those rows.',
                      style: const TextStyle(
                        color: AppColors.orange,
                        fontSize: 13,
                      ),
                    ),
                  ],
                  const SizedBox(height: 16),
                  const Text(
                    'What you can do',
                    style: TextStyle(
                      color: AppColors.textPrimary,
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
                    decoration: AppDecorations.subtleBorder(radius: 12),
                    child: Column(
                      children: [
                        _canDoRow(
                          allowed: canEdit,
                          action: 'Edit',
                          detail: s.isCancelled
                              ? 'Cancelled shows are frozen.'
                              : hasHistory
                                  ? 'Booking history freezes the sold show, including language. Cancel the show if it is still upcoming.'
                                  : 'Movie, hall, time, price and language can all change.',
                        ),
                        _canDoRow(
                          allowed: canCancelShow,
                          action: 'Cancel show',
                          detail: s.isCancelled
                              ? 'Already cancelled.'
                              : s.isUpcoming
                                  ? 'Refunds active tickets. This row stays in history.'
                                  : 'The show has already started.',
                        ),
                        _canDoRow(
                          allowed: canDelete,
                          action: 'Delete',
                          detail: canDelete
                              ? 'No booking history, so this row can be removed.'
                              : s.isUpcoming && !s.isCancelled
                                  ? 'Booking history must stay. Cancel the show instead.'
                                  : 'Booking history must stay.',
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              ),
            ),
            actions: [
              if (hasHistory)
                TextButton(
                  onPressed: () {
                    Navigator.pop(context);
                    if (s.id != null) {
                      context.read<ReservationProvider>().focusProjection(s.id!);
                      widget.onNavigate?.call(4);
                    }
                  },
                  child: const Text('Open bookings'),
                ),
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
            ],
          );
        },
      ),
    );
  }

  String _bookingSummary(Projection s, List<Reservation> tickets) {
    if (tickets.isEmpty) {
      return s.hasBookings ? 'Has booking history' : 'None';
    }
    final parts = <String>[];
    void add(String label, int status) {
      final count = tickets.where((t) => t.status == status).length;
      if (count > 0) parts.add('$count $label');
    }

    add('pending', ReservationStatus.pending);
    add('confirmed', ReservationStatus.confirmed);
    add('paid', ReservationStatus.paid);
    add('completed', ReservationStatus.completed);
    add('cancelled', ReservationStatus.cancelled);
    return parts.isEmpty ? '${tickets.length}' : '${tickets.length} · ${parts.join(', ')}';
  }

  Widget _infoLine(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 84,
            child: Text(
              label,
              style: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(color: AppColors.textPrimary, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }

  Widget _canDoRow({
    required bool allowed,
    required String action,
    required String detail,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            allowed ? Icons.check_circle_outline : Icons.block,
            size: 16,
            color: allowed ? AppColors.green : AppColors.orange,
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 108,
            child: Text(
              action,
              style: const TextStyle(
                color: AppColors.textPrimary,
                fontSize: 13,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Expanded(
            child: Text(
              '${allowed ? 'Allowed' : 'Not allowed'}. $detail',
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            ),
          ),
        ],
      ),
    );
  }

  /// Same booking-history gate as delete: no field can change once a reservation exists.
  Future<void> _edit(Projection s) async {
    if (s.isCancelled) {
      showAppSnackBar(context, 'Cancelled projections cannot be edited.', isError: true);
      return;
    }
    if (s.id == null) {
      await _showDialog(projection: s);
      return;
    }

    Map<String, dynamic>? impact;
    try {
      impact = await _provider.getDeleteImpact(s.id!);
    } on Exception catch (_) {}

    if (!mounted) return;
    final blocked = cascadeDeleteBlockReason(impact);
    final hasHistory = blocked != null || (impact == null && s.hasBookings);
    if (hasHistory) {
      final choice = await _blockedEditChoice();
      if (choice == 'bookings') {
        context.read<ReservationProvider>().focusProjection(s.id!);
        widget.onNavigate?.call(4);
      }
      return;
    }

    await _showDialog(projection: s);
  }

  Future<String?> _blockedEditChoice() {
    return showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        title: const Text(
          'Cannot edit',
          style: TextStyle(color: AppColors.textPrimary),
        ),
        content: const SizedBox(
          width: 460,
          child: Text(
            'This projection has booking records — the same history that blocks delete — '
            'so it cannot be changed at all, including language. Cancel the show if it is '
            'still upcoming; the sold details stay on record.',
            style: TextStyle(color: AppColors.textSecondary),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, 'bookings'),
            child: const Text('View bookings'),
          ),
        ],
      ),
    );
  }

  /// A sold or held projection cannot be hard-deleted. Offer the Bookings list
  /// (where the blocking row actually lives) and Cancel when the show is still upcoming.
  Future<String?> _blockedDeleteChoice(Projection s, String blocked) {
    final canCancelShow = s.isUpcoming && !s.isCancelled;
    return showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        title: const Text(
          'Cannot delete',
          style: TextStyle(color: AppColors.textPrimary),
        ),
        content: SizedBox(
          width: 460,
          child: Text(
            canCancelShow
                ? blocked
                : '$blocked A projection that has already started or was cancelled cannot be removed.',
            style: const TextStyle(color: AppColors.textSecondary),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, 'bookings'),
            child: const Text('View bookings'),
          ),
          if (canCancelShow)
            ElevatedButton(
              onPressed: () => Navigator.pop(context, 'cancel'),
              child: const Text('Cancel projection'),
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
      final choice = await _blockedDeleteChoice(s, blocked);
      if (choice == 'bookings' && s.id != null) {
        context.read<ReservationProvider>().focusProjection(s.id!);
        widget.onNavigate?.call(4);
      } else if (choice == 'cancel') {
        await _cancel(
          s,
          extraMessage:
              'Customers are refunded and the sold tickets stay on record.',
        );
      }
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

    int? movieId = projection?.movieId;
    int? hallId = projection?.hallId ?? _defaultActiveHallId;
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
                      ? 'Halls whose status blocks projections cannot be selected.'
                      : null,
                ),
                // Unavailable halls stay visible but greyed out, with the status
                // spelled out, rather than being selectable and rejected afterwards.
                items: _hallsForFilter.map((h) {
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
                        final now = DateTime.now();
                        final firstDate = (date != null && date!.isBefore(now))
                            ? date!
                            : now;
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: date ?? now,
                          firstDate: firstDate,
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
                decoration: const InputDecoration(
                  labelText: 'Price',
                  hintText: 'e.g. \$15',
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
