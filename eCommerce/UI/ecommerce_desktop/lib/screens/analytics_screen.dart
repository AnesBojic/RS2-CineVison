import 'dart:math' as math;
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/analytics.dart';
import 'package:ecommerce_desktop/providers/analytics_provider.dart';
import 'package:ecommerce_desktop/utils/analytics_pdf_reports.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

class AnalyticsScreen extends StatefulWidget {
  const AnalyticsScreen({super.key});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen> {
  bool _loading = true;
  bool _exportingPdf = false;
  final _scrollController = ScrollController();
  final _movieFilterCtrl = TextEditingController();
  final _slotFilterCtrl = TextEditingController();
  final _hallFilterCtrl = TextEditingController();
  List<MoviePerformance> _movies = [];
  List<TimeSlotPerformance> _timeSlots = [];
  List<HallUtilization> _halls = [];

  List<MoviePerformance> get _filteredMovies {
    final q = _movieFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _movies;
    return _movies.where((m) => m.title.toLowerCase().contains(q)).toList();
  }

  List<TimeSlotPerformance> get _filteredSlots {
    final q = _slotFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _timeSlots;
    return _timeSlots.where((s) => s.timeSlot.toLowerCase().contains(q)).toList();
  }

  List<HallUtilization> get _filteredHalls {
    final q = _hallFilterCtrl.text.trim().toLowerCase();
    if (q.isEmpty) return _halls;
    return _halls.where((h) => h.hallName.toLowerCase().contains(q)).toList();
  }

  @override
  void initState() {
    super.initState();
    context.read<AnalyticsProvider>().addListener(_onLiveAnalytics);
    final snapshot = context.read<AnalyticsProvider>().liveSnapshot;
    if (snapshot != null) {
      _applySnapshot(snapshot);
    } else {
      _load();
    }
  }

  @override
  void dispose() {
    context.read<AnalyticsProvider>().removeListener(_onLiveAnalytics);
    _scrollController.dispose();
    _movieFilterCtrl.dispose();
    _slotFilterCtrl.dispose();
    _hallFilterCtrl.dispose();
    super.dispose();
  }

  void _onLiveAnalytics() {
    final snapshot = context.read<AnalyticsProvider>().liveSnapshot;
    if (snapshot != null) {
      _applySnapshot(snapshot);
    }
  }

  void _applySnapshot(AnalyticsLiveSnapshot snapshot) {
    setState(() {
      _movies = snapshot.moviePerformance;
      _timeSlots = snapshot.timeSlotPerformance;
      _halls = snapshot.hallUtilization;
      _loading = false;
    });
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final provider = context.read<AnalyticsProvider>();
      final results = await Future.wait([
        provider.getMoviePerformance(),
        provider.getTimeSlotPerformance(),
        provider.getHallUtilization(),
      ]);
      if (!mounted) return;
      setState(() {
        _movies = results[0] as List<MoviePerformance>;
        _timeSlots = results[1] as List<TimeSlotPerformance>;
        _halls = results[2] as List<HallUtilization>;
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
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
            Row(
              children: [
                const Text(
                  'Analytics Dashboard',
                  style: TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(width: 12),
                Consumer<AnalyticsProvider>(
                  builder: (context, analytics, _) {
                    if (!analytics.isLiveConnected) {
                      return const SizedBox.shrink();
                    }
                    return Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppColors.green.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: AppColors.green.withValues(alpha: 0.4)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 8,
                            height: 8,
                            decoration: const BoxDecoration(
                              color: AppColors.green,
                              shape: BoxShape.circle,
                            ),
                          ),
                          const SizedBox(width: 6),
                          const Text(
                            'Live',
                            style: TextStyle(
                              color: AppColors.green,
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                ),
              ],
            ),
            const SizedBox(height: 20),
            _buildPdfReportsCard(),
            const SizedBox(height: 28),
            SectionHeader(
              title: 'Most Popular Movies',
              action: SizedBox(
                width: 220,
                child: SearchField(
                  controller: _movieFilterCtrl,
                  hint: 'Filter movies',
                  onChanged: (_) => setState(() {}),
                ),
              ),
            ),
            DataCard(
              emptyMessage: _filteredMovies.isEmpty ? 'No performance data yet' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Total Tickets')),
                  DataColumn(label: Text('Revenue')),
                  DataColumn(label: Text('Occupancy')),
                  DataColumn(label: Text('Avg Rating')),
                ],
                rows: _filteredMovies.map((m) {
                  return DataRow(cells: [
                    DataCell(Row(children: [
                      posterThumbnail(m.posterImageBase64),
                      const SizedBox(width: 12),
                      Text(m.title, style: const TextStyle(fontWeight: FontWeight.w500)),
                    ])),
                    DataCell(Text('${m.ticketsSold}')),
                    DataCell(Text(
                      formatCurrency(m.revenue),
                      style: const TextStyle(color: AppColors.green, fontWeight: FontWeight.w700),
                    )),
                    DataCell(occupancyBar(m.occupancyPercent, AppColors.green)),
                    DataCell(RatingChip(rating: m.avgRating)),
                  ]);
                }).toList(),
              ),
            ),
            const SizedBox(height: 28),
            SectionHeader(
              title: 'Performance by Time Slot',
              action: SizedBox(
                width: 220,
                child: SearchField(
                  controller: _slotFilterCtrl,
                  hint: 'Filter slots',
                  onChanged: (_) => setState(() {}),
                ),
              ),
            ),
            DataCard(
              emptyMessage: _filteredSlots.isEmpty ? 'No time slot data yet' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Time Slot')),
                  DataColumn(label: Text('Tickets Sold')),
                  DataColumn(label: Text('Occupancy')),
                  DataColumn(label: Text('Revenue')),
                ],
                rows: _filteredSlots.map((s) {
                  return DataRow(cells: [
                    DataCell(Text(s.timeSlot)),
                    DataCell(Text('${s.ticketsSold}')),
                    DataCell(occupancyBar(s.occupancyPercent, AppColors.blue, width: 140)),
                    DataCell(Text(
                      formatCurrency(s.revenue),
                      style: const TextStyle(color: AppColors.green, fontWeight: FontWeight.w700),
                    )),
                  ]);
                }).toList(),
              ),
            ),
            const SizedBox(height: 28),
            SectionHeader(
              title: 'Hall Usage Distribution',
              action: SizedBox(
                width: 220,
                child: SearchField(
                  controller: _hallFilterCtrl,
                  hint: 'Filter halls',
                  onChanged: (_) => setState(() {}),
                ),
              ),
            ),
            DataCard(
              child: Padding(
                padding: const EdgeInsets.all(28),
                child: _filteredHalls.isEmpty
                    ? const Center(
                        child: Text('No hall usage data yet', style: TextStyle(color: AppColors.textSecondary)),
                      )
                    : Column(
                        children: [
                          SizedBox(height: 240, child: _HallPieChart(halls: _filteredHalls)),
                          const SizedBox(height: 28),
                          Wrap(
                            spacing: 28,
                            runSpacing: 14,
                            alignment: WrapAlignment.center,
                            children: _filteredHalls.asMap().entries.map((entry) {
                              final h = entry.value;
                              return _HallLegendItem(
                                color: _hallColor(entry.key),
                                label: h.hallName,
                                value: '${h.showCount} shows',
                              );
                            }).toList(),
                          ),
                        ],
                      ),
              ),
            ),
          ],
        ),
        ),
      ),
    );
  }

  Color _hallColor(int index) {
    const colors = [AppColors.primary, AppColors.purple, AppColors.blue, AppColors.green];
    return colors[index % colors.length];
  }

  Widget _buildPdfReportsCard() {
    return DataCard(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'PDF reports',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Download or print analytics reports in PDF format.',
              style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
            ),
            const SizedBox(height: 16),
            if (_exportingPdf)
              const Padding(
                padding: EdgeInsets.only(bottom: 12),
                child: LinearProgressIndicator(color: AppColors.primary),
              ),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                _PdfReportActions(
                  title: 'Movie Performance',
                  subtitle: 'Tickets, revenue, occupancy, ratings',
                  enabled: !_exportingPdf,
                  onPrint: () => _exportMovieReport(print: true),
                  onDownload: () => _exportMovieReport(print: false),
                ),
                _PdfReportActions(
                  title: 'Hall Utilization',
                  subtitle: 'Hall usage and time-slot performance',
                  enabled: !_exportingPdf,
                  onPrint: () => _exportHallReport(print: true),
                  onDownload: () => _exportHallReport(print: false),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String _stamp() => DateFormat('yyyyMMdd_HHmm').format(DateTime.now());

  Future<void> _exportMovieReport({required bool print}) async {
    await _runPdfExport(() async {
      final bytes = await AnalyticsPdfReports.buildMoviePerformanceReport(_movies);
      final name = 'cinevision_movie_performance_${_stamp()}.pdf';
      if (print) {
        await AnalyticsPdfReports.printPdf(bytes, name: name);
      } else {
        await _savePdf(bytes, name);
      }
    });
  }

  Future<void> _exportHallReport({required bool print}) async {
    await _runPdfExport(() async {
      final bytes = await AnalyticsPdfReports.buildHallUtilizationReport(
        _halls,
        _timeSlots,
      );
      final name = 'cinevision_hall_utilization_${_stamp()}.pdf';
      if (print) {
        await AnalyticsPdfReports.printPdf(bytes, name: name);
      } else {
        await _savePdf(bytes, name);
      }
    });
  }

  Future<void> _savePdf(Uint8List bytes, String fileName) async {
    final path = await AnalyticsPdfReports.downloadPdf(
      bytes,
      suggestedFileName: fileName,
    );
    if (!mounted) return;
    if (path == null || path.isEmpty) {
      showAppSnackBar(context, 'Save cancelled');
      return;
    }
    showAppSnackBar(context, 'PDF saved: $path');
  }

  Future<void> _runPdfExport(Future<void> Function() action) async {
    if (_exportingPdf) return;
    setState(() => _exportingPdf = true);
    try {
      await action();
    } on Exception catch (e) {
      if (mounted) {
        showAppSnackBar(context, 'PDF export failed: $e', isError: true);
      }
    } finally {
      if (mounted) setState(() => _exportingPdf = false);
    }
  }
}

class _PdfReportActions extends StatelessWidget {
  const _PdfReportActions({
    required this.title,
    required this.subtitle,
    required this.enabled,
    required this.onPrint,
    required this.onDownload,
  });

  final String title;
  final String subtitle;
  final bool enabled;
  final VoidCallback onPrint;
  final VoidCallback onDownload;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 320,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontWeight: FontWeight.w700,
              fontSize: 14,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            subtitle,
            style: const TextStyle(color: AppColors.textSecondary, fontSize: 12),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: enabled ? onDownload : null,
                  icon: const Icon(Icons.download_outlined, size: 16),
                  label: const Text('Download'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: enabled ? onPrint : null,
                  icon: const Icon(Icons.print_outlined, size: 16),
                  label: const Text('Print'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _HallLegendItem extends StatelessWidget {
  const _HallLegendItem({required this.color, required this.label, required this.value});

  final Color color;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 8),
        Text('$label: $value', style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
      ],
    );
  }
}

class _HallPieChart extends StatelessWidget {
  const _HallPieChart({required this.halls});

  final List<HallUtilization> halls;

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      painter: _PieChartPainter(halls: halls),
      child: const SizedBox.expand(),
    );
  }
}

class _PieChartPainter extends CustomPainter {
  _PieChartPainter({required this.halls});

  final List<HallUtilization> halls;
  final _colors = [AppColors.primary, AppColors.purple, AppColors.blue, AppColors.green];

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = math.min(size.width, size.height) / 2 - 24;
    final total = halls.fold<double>(0, (sum, h) => sum + h.sharePercent);
    var startAngle = -math.pi / 2;

    for (var i = 0; i < halls.length; i++) {
      final sweep = total > 0
          ? (halls[i].sharePercent / total) * 2 * math.pi
          : (2 * math.pi / halls.length);
      final paint = Paint()
        ..color = _colors[i % _colors.length]
        ..style = PaintingStyle.fill;
      canvas.drawArc(
        Rect.fromCircle(center: center, radius: radius),
        startAngle,
        sweep,
        true,
        paint,
      );

      final labelAngle = startAngle + sweep / 2;
      final labelOffset = Offset(
        center.dx + math.cos(labelAngle) * radius * 0.62,
        center.dy + math.sin(labelAngle) * radius * 0.62,
      );
      final textPainter = TextPainter(
        text: TextSpan(
          text: '${halls[i].sharePercent.toStringAsFixed(0)}%',
          style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w700),
        ),
        textDirection: ui.TextDirection.ltr,
      )..layout();
      textPainter.paint(
        canvas,
        labelOffset - Offset(textPainter.width / 2, textPainter.height / 2),
      );

      startAngle += sweep;
    }

    canvas.drawCircle(center, radius * 0.42, Paint()..color = AppColors.card);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => true;
}
