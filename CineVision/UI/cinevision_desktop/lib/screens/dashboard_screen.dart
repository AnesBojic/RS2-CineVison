import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/analytics.dart';
import 'package:cinevision_desktop/models/hall.dart';
import 'package:cinevision_desktop/models/movie.dart';
import 'package:cinevision_desktop/models/screening.dart';
import 'package:cinevision_desktop/models/search_result.dart';
import 'package:cinevision_desktop/providers/analytics_provider.dart';
import 'package:cinevision_desktop/providers/hall_provider.dart';
import 'package:cinevision_desktop/providers/movie_provider.dart';
import 'package:cinevision_desktop/providers/screening_provider.dart';
import 'package:cinevision_desktop/utils/api_client_exception.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key, this.onNavigate});

  final void Function(int index, {int? editId})? onNavigate;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  bool _loading = true;
  final _scrollController = ScrollController();
  final _movieFilterCtrl = TextEditingController();
  final _hallFilterCtrl = TextEditingController();
  final _screeningFilterCtrl = TextEditingController();
  DashboardStats? _dashboard;
  List<Movie> _movies = [];
  List<Hall> _halls = [];
  List<Screening> _screenings = [];

  List<Movie> get _filteredMovies {
    final q = _movieFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _movies;
    return _movies.where((m) {
      final title = (m.title ?? '').toLowerCase();
      final genre = (m.genre?.name ?? '').toLowerCase();
      return title.contains(q) || genre.contains(q);
    }).toList();
  }

  List<Hall> get _filteredHalls {
    final q = _hallFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _halls;
    return _halls.where((h) => (h.name ?? '').toLowerCase().contains(q)).toList();
  }

  List<Screening> get _filteredScreenings {
    final q = _screeningFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _screenings;
    return _screenings.where((s) {
      final movie = (_movieById(s.movieId)?.title ?? s.movieTitle ?? '').toLowerCase();
      final hall = (s.hallName ?? '').toLowerCase();
      return movie.contains(q) || hall.contains(q);
    }).toList();
  }

  @override
  void initState() {
    super.initState();
    context.read<AnalyticsProvider>().addListener(_onLiveAnalytics);
    _load();
  }

  @override
  void dispose() {
    context.read<AnalyticsProvider>().removeListener(_onLiveAnalytics);
    _scrollController.dispose();
    _movieFilterCtrl.dispose();
    _hallFilterCtrl.dispose();
    _screeningFilterCtrl.dispose();
    super.dispose();
  }

  void _onLiveAnalytics() {
    final live = context.read<AnalyticsProvider>().liveDashboard;
    if (live != null && mounted) {
      setState(() => _dashboard = live);
    }
  }

  Future<void> _load() async {
    setState(() => _loading = true);

    final movieProvider = context.read<MovieProvider>();
    final hallProvider = context.read<HallProvider>();
    final screeningProvider = context.read<ScreeningProvider>();
    final analyticsProvider = context.read<AnalyticsProvider>();

    DashboardStats? dashboard;
    List<Movie> movies = [];
    List<Hall> halls = [];
    List<Screening> screenings = [];
    String? analyticsError;

    try {
      final catalog = await Future.wait([
        movieProvider.get(
          filter: {'pageSize': 100, 'includeGenre': true},
          includePoster: true,
        ),
        hallProvider.get(filter: {'pageSize': 5}),
        screeningProvider.get(
          filter: {
            'pageSize': 6,
            'includeSeatStats': false,
          },
        ),
      ]);
      movies = (catalog[0] as SearchResult<Movie>).items ?? [];
      halls = (catalog[1] as SearchResult<Hall>).items ?? [];
      screenings = (catalog[2] as SearchResult<Screening>).items ?? [];
      movies.sort((a, b) => (b.id ?? 0).compareTo(a.id ?? 0));
      halls.sort((a, b) => (b.id ?? 0).compareTo(a.id ?? 0));
      screenings.sort((a, b) {
        final at = a.startTime ?? DateTime.fromMillisecondsSinceEpoch(0);
        final bt = b.startTime ?? DateTime.fromMillisecondsSinceEpoch(0);
        return bt.compareTo(at);
      });
    } on Exception catch (e) {
      if (mounted) {
        alertBox(context, 'Error', e.toString());
      }
    }

    try {
      dashboard = await analyticsProvider.getDashboard();
      final live = analyticsProvider.liveDashboard;
      if (live != null) {
        dashboard = live;
      }
    } on Exception {
      analyticsError = 'Analytics could not be loaded.';
    }

    if (!mounted) return;
    setState(() {
      _dashboard = dashboard;
      _movies = movies;
      _halls = halls;
      _screenings = screenings;
      _loading = false;
    });

    if (analyticsError != null && mounted) {
      showAppSnackBar(context, analyticsError!, isError: true);
    }
  }

  Movie? _movieById(int? id) {
    if (id == null) return null;
    for (final movie in _movies) {
      if (movie.id == id) return movie;
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator(color: AppColors.primary));
    }

    return RefreshIndicator(
      color: AppColors.primary,
      backgroundColor: AppColors.card,
      onRefresh: _load,
      child: Scrollbar(
        controller: _scrollController,
        thumbVisibility: true,
        child: SingleChildScrollView(
          controller: _scrollController,
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(32, 24, 32, 32),
          child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildStats(),
            const SizedBox(height: 28),
            _buildMoviesSection(),
            const SizedBox(height: 28),
            _buildHallsSection(),
            const SizedBox(height: 28),
            _buildProjectionsSection(),
          ],
        ),
        ),
      ),
    );
  }

  Widget _buildStats() {
    final d = _dashboard;
    return Row(
      children: [
        StatCard(
          icon: Icons.tv,
          iconColor: AppColors.primary,
          value: '${d?.totalScreenings ?? 0}',
          label: 'Total Screens',
          subtitle: 'Active',
        ),
        const SizedBox(width: 14),
        StatCard(
          icon: Icons.movie,
          iconColor: AppColors.purple,
          value: '${d?.activeMovies ?? 0}',
          label: 'Total Movies',
          subtitle: 'Now Showing',
        ),
        const SizedBox(width: 14),
        StatCard(
          icon: Icons.calendar_today,
          iconColor: AppColors.blue,
          value: '${d?.upcomingScreenings ?? 0}',
          label: 'Total Upcoming',
          subtitle: 'This Month',
        ),
        const SizedBox(width: 14),
        StatCard(
          icon: Icons.confirmation_number,
          iconColor: AppColors.green,
          value: _formatCount(d?.totalTicketsSold ?? 0),
          label: 'Total Tickets',
          subtitle: 'This Month',
        ),
        const SizedBox(width: 14),
        StatCard(
          icon: Icons.people,
          iconColor: AppColors.orange,
          value: _formatCount(d?.totalCustomers ?? 0),
          label: 'Total Customers',
          subtitle: 'Registered',
        ),
      ],
    );
  }

  String _formatCount(int value) {
    if (value >= 1000) return '${(value / 1000).toStringAsFixed(1)}k';
    return '$value';
  }

  Widget _actionsCell({
    required VoidCallback onEdit,
    required VoidCallback onDelete,
  }) {
    return Align(
      alignment: Alignment.centerRight,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          ActionIconButton(
            icon: Icons.edit_outlined,
            color: AppColors.blue,
            onPressed: onEdit,
          ),
          const SizedBox(width: 8),
          ActionIconButton(
            icon: Icons.delete_outline,
            color: AppColors.primary,
            onPressed: onDelete,
          ),
        ],
      ),
    );
  }

  Future<void> _deleteMovie(Movie m) async {
    if (m.id == null) return;
    final provider = context.read<MovieProvider>();
    Map<String, dynamic>? impact;
    try {
      impact = await provider.getDeleteImpact(m.id!);
    } on Exception catch (_) {}
    if (!mounted) return;
    final ok = await confirmDelete(
      context,
      buildCascadeDeleteWarning(subjectLabel: '"${m.title}"', impact: impact),
    );
    if (ok != true || !mounted) return;
    try {
      await provider.remove(m.id!);
      showAppSnackBar(context, 'Movie and related data deleted');
      _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot delete', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _deleteHall(Hall h) async {
    if (h.id == null) return;
    final provider = context.read<HallProvider>();
    Map<String, dynamic>? impact;
    try {
      impact = await provider.getDeleteImpact(h.id!);
    } on Exception catch (_) {}
    if (!mounted) return;
    final ok = await confirmDelete(
      context,
      buildCascadeDeleteWarning(subjectLabel: '"${h.name}"', impact: impact),
    );
    if (ok != true || !mounted) return;
    try {
      await provider.remove(h.id!);
      showAppSnackBar(context, 'Hall and related data deleted');
      _load();
    } on ApiClientException catch (e) {
      if (mounted) showAppSnackBar(context, e.message, isError: true);
    } on Exception catch (e) {
      if (mounted) showAppSnackBar(context, e.toString(), isError: true);
    }
  }

  Future<void> _deleteScreening(Screening s) async {
    if (s.id == null) return;
    final provider = context.read<ScreeningProvider>();
    Map<String, dynamic>? impact;
    try {
      impact = await provider.getDeleteImpact(s.id!);
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
      await provider.remove(s.id!);
      showAppSnackBar(context, 'Projection and related bookings deleted');
      _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Widget _buildMoviesSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SectionHeader(
          title: 'Manage Movies',
          action: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              SearchField(
                controller: _movieFilterCtrl,
                hint: 'Filter movies',
                width: 200,
                onChanged: (_) => setState(() {}),
              ),
              const SizedBox(width: 10),
              PrimaryButton(
                label: 'Add Movie',
                compact: true,
                onPressed: () => widget.onNavigate?.call(1),
              ),
            ],
          ),
        ),
        DataCard(
          emptyMessage: _filteredMovies.isEmpty ? 'No movies yet' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Movie Title')),
              DataColumn(label: Text('Genre')),
              DataColumn(label: Text('Rating')),
              DataColumn(label: Text('Duration')),
              DataColumn(label: Text('Release Date')),
              DataColumn(label: Text('Status')),
              actionsDataColumn,
            ],
            rows: _filteredMovies.map((m) {
              return DataRow(cells: [
                DataCell(Row(children: [
                  posterThumbnail(m.posterImageBase64),
                  const SizedBox(width: 12),
                  Text(m.title ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
                ])),
                DataCell(Text(m.genre?.name ?? '—')),
                DataCell(Text(m.ageRating ?? '—')),
                DataCell(Text('${m.durationMinutes ?? 0} min')),
                DataCell(Text(formatDate(m.releaseDate))),
                DataCell(StatusBadge(
                  label: m.displayState,
                  color: m.isActiveState ? AppColors.green : AppColors.orange,
                  filled: true,
                )),
                DataCell(_actionsCell(
                  onEdit: () => widget.onNavigate?.call(1, editId: m.id),
                  onDelete: () => _deleteMovie(m),
                )),
              ]);
            }).toList(),
          ),
        ),
      ],
    );
  }

  Widget _buildHallsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SectionHeader(
          title: 'Manage Halls',
          action: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              SearchField(
                controller: _hallFilterCtrl,
                hint: 'Filter halls',
                width: 200,
                onChanged: (_) => setState(() {}),
              ),
              const SizedBox(width: 10),
              PrimaryButton(
                label: 'Add Hall',
                compact: true,
                onPressed: () => widget.onNavigate?.call(2),
              ),
            ],
          ),
        ),
        DataCard(
          emptyMessage: _filteredHalls.isEmpty ? 'No halls yet' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Hall Name')),
              DataColumn(label: Text('Layout')),
              DataColumn(label: Text('Screen Type')),
              DataColumn(label: Text('Status')),
              actionsDataColumn,
            ],
            rows: _filteredHalls.map((h) {
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
                  label: h.statusName ?? 'Active',
                  color: hallStatusColor(h),
                  filled: true,
                )),
                DataCell(_actionsCell(
                  onEdit: () => widget.onNavigate?.call(2, editId: h.id),
                  onDelete: () => _deleteHall(h),
                )),
              ]);
            }).toList(),
          ),
        ),
      ],
    );
  }

  Widget _buildProjectionsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SectionHeader(
          title: 'Manage Projections',
          action: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              SearchField(
                controller: _screeningFilterCtrl,
                hint: 'Filter projections',
                width: 200,
                onChanged: (_) => setState(() {}),
              ),
              const SizedBox(width: 10),
              PrimaryButton(
                label: 'Add Projection',
                compact: true,
                onPressed: () => widget.onNavigate?.call(3),
              ),
            ],
          ),
        ),
        DataCard(
          emptyMessage: _filteredScreenings.isEmpty ? 'No projections scheduled' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Movie')),
              DataColumn(label: Text('Hall')),
              DataColumn(label: Text('Date')),
              DataColumn(label: Text('Time')),
              DataColumn(label: Text('Price')),
              actionsDataColumn,
            ],
            rows: _filteredScreenings.map((s) {
              final movie = _movieById(s.movieId);
              final poster = movie?.posterImageBase64 ?? s.moviePosterBase64;
              return DataRow(cells: [
                DataCell(Row(children: [
                  posterThumbnail(poster),
                  const SizedBox(width: 12),
                  Text(
                    movie?.title ?? s.movieTitle ?? '—',
                    style: const TextStyle(fontWeight: FontWeight.w500),
                  ),
                ])),
                DataCell(Text(s.hallName ?? '—')),
                DataCell(Text(formatDate(s.startTime))),
                DataCell(Text(formatTime(s.startTime))),
                DataCell(Text(formatCurrency(s.basePrice))),
                DataCell(_actionsCell(
                  onEdit: () => widget.onNavigate?.call(3, editId: s.id),
                  onDelete: () => _deleteScreening(s),
                )),
              ]);
            }).toList(),
          ),
        ),
      ],
    );
  }
}
