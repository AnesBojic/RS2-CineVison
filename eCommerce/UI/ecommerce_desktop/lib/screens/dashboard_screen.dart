import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/analytics.dart';
import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/models/movie.dart';
import 'package:ecommerce_desktop/models/screening.dart';
import 'package:ecommerce_desktop/providers/analytics_provider.dart';
import 'package:ecommerce_desktop/providers/hall_provider.dart';
import 'package:ecommerce_desktop/providers/movie_provider.dart';
import 'package:ecommerce_desktop/providers/screening_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
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
  DashboardStats? _dashboard;
  List<Movie> _movies = [];
  List<Hall> _halls = [];
  List<Screening> _screenings = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final analyticsProvider = context.read<AnalyticsProvider>();
      final movieProvider = context.read<MovieProvider>();
      final hallProvider = context.read<HallProvider>();
      final screeningProvider = context.read<ScreeningProvider>();

      final dashboard = await analyticsProvider.getDashboard();
      final movies = await movieProvider.get(
        filter: {'pageSize': 100, 'includeGenre': true},
        includePoster: true,
      );
      final halls = await hallProvider.get(filter: {'pageSize': 5});
      final screenings = await screeningProvider.get(
        filter: {'pageSize': 6, 'includeMovie': true, 'includeHall': true},
      );
      if (!mounted) return;
      setState(() {
        _dashboard = dashboard;
        _movies = movies.items ?? [];
        _halls = halls.items ?? [];
        _screenings = screenings.items ?? [];
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
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
          value: _formatCount(d?.totalReservations ?? 0),
          label: 'Total Customers',
          subtitle: 'Active',
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
    return Row(children: [
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
    ]);
  }

  Future<void> _deleteMovie(Movie m) async {
    final ok = await confirmDelete(context, 'Delete "${m.title}"?');
    if (ok != true || !mounted) return;
    try {
      await context.read<MovieProvider>().remove(m.id!);
      showAppSnackBar(context, 'Movie deleted');
      _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot delete', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _deleteHall(Hall h) async {
    final ok = await confirmDelete(context, 'Delete "${h.name}"?');
    if (ok != true || !mounted) return;
    try {
      await context.read<HallProvider>().remove(h.id!);
      showAppSnackBar(context, 'Hall deleted');
      _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _deleteScreening(Screening s) async {
    final ok = await confirmDelete(context, 'Delete this projection?');
    if (ok != true || !mounted) return;
    try {
      await context.read<ScreeningProvider>().remove(s.id!);
      showAppSnackBar(context, 'Projection deleted');
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
          action: PrimaryButton(
            label: 'Add Movie',
            compact: true,
            onPressed: () => widget.onNavigate?.call(1),
          ),
        ),
        DataCard(
          emptyMessage: _movies.isEmpty ? 'No movies yet' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Movie Title')),
              DataColumn(label: Text('Genre')),
              DataColumn(label: Text('Rating')),
              DataColumn(label: Text('Duration')),
              DataColumn(label: Text('Release Date')),
              DataColumn(label: Text('Status')),
              DataColumn(label: Text('Actions')),
            ],
            rows: _movies.map((m) {
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
          action: PrimaryButton(
            label: 'Add Hall',
            compact: true,
            onPressed: () => widget.onNavigate?.call(2),
          ),
        ),
        DataCard(
          emptyMessage: _halls.isEmpty ? 'No halls yet' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Hall Name')),
              DataColumn(label: Text('Layout')),
              DataColumn(label: Text('Screen Type')),
              DataColumn(label: Text('Status')),
              DataColumn(label: Text('Actions')),
            ],
            rows: _halls.map((h) {
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
                  color: hallStatusColor(h.status),
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
          action: PrimaryButton(
            label: 'Add Projection',
            compact: true,
            onPressed: () => widget.onNavigate?.call(3),
          ),
        ),
        DataCard(
          emptyMessage: _screenings.isEmpty ? 'No projections scheduled' : null,
          child: StyledDataTable(
            columns: const [
              DataColumn(label: Text('Movie')),
              DataColumn(label: Text('Hall')),
              DataColumn(label: Text('Date')),
              DataColumn(label: Text('Time')),
              DataColumn(label: Text('Price')),
              DataColumn(label: Text('Actions')),
            ],
            rows: _screenings.map((s) {
              final poster = s.moviePosterBase64?.isNotEmpty == true
                  ? s.moviePosterBase64
                  : _movieById(s.movieId)?.posterImageBase64;
              return DataRow(cells: [
                DataCell(Row(children: [
                  posterThumbnail(poster),
                  const SizedBox(width: 12),
                  Text(s.movieTitle ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
                ])),
                DataCell(Text(s.hallName ?? '—')),
                DataCell(Text(formatDate(s.startTime))),
                DataCell(StatusBadge(
                  label: formatTime(s.startTime),
                  color: AppColors.green,
                  filled: true,
                )),
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
