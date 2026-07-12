import 'dart:math' as math;

import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/analytics.dart';
import 'package:ecommerce_desktop/providers/analytics_provider.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AnalyticsScreen extends StatefulWidget {
  const AnalyticsScreen({super.key});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen> {
  bool _loading = true;
  final _scrollController = ScrollController();
  List<MoviePerformance> _movies = [];
  List<TimeSlotPerformance> _timeSlots = [];
  List<HallUtilization> _halls = [];

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
    setState(() => _loading = true);
    try {
      final provider = context.read<AnalyticsProvider>();
      final movies = await provider.getMoviePerformance();
      final slots = await provider.getTimeSlotPerformance();
      final halls = await provider.getHallUtilization();
      if (!mounted) return;
      setState(() {
        _movies = movies;
        _timeSlots = slots;
        _halls = halls;
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
            const Text(
              'Analytics Dashboard',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: 22,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 24),
            const SectionHeader(title: 'Most Popular Movies'),
            DataCard(
              emptyMessage: _movies.isEmpty ? 'No performance data yet' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Movie')),
                  DataColumn(label: Text('Total Tickets')),
                  DataColumn(label: Text('Revenue')),
                  DataColumn(label: Text('Occupancy')),
                  DataColumn(label: Text('Avg Rating')),
                ],
                rows: _movies.map((m) {
                  return DataRow(cells: [
                    DataCell(Row(children: [
                      posterThumbnail(null),
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
            const SectionHeader(title: 'Performance by Time Slot'),
            DataCard(
              emptyMessage: _timeSlots.isEmpty ? 'No time slot data yet' : null,
              child: StyledDataTable(
                columns: const [
                  DataColumn(label: Text('Time Slot')),
                  DataColumn(label: Text('Tickets Sold')),
                  DataColumn(label: Text('Occupancy')),
                  DataColumn(label: Text('Revenue')),
                ],
                rows: _timeSlots.map((s) {
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
            const SectionHeader(title: 'Hall Usage Distribution'),
            DataCard(
              child: Padding(
                padding: const EdgeInsets.all(28),
                child: _halls.isEmpty
                    ? const Center(
                        child: Text('No hall usage data yet', style: TextStyle(color: AppColors.textSecondary)),
                      )
                    : Column(
                        children: [
                          SizedBox(height: 240, child: _HallPieChart(halls: _halls)),
                          const SizedBox(height: 28),
                          Wrap(
                            spacing: 28,
                            runSpacing: 14,
                            alignment: WrapAlignment.center,
                            children: _halls.asMap().entries.map((entry) {
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
        textDirection: TextDirection.ltr,
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
