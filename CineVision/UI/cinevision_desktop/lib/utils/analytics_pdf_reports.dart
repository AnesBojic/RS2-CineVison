import 'dart:typed_data';

import 'package:cinevision_desktop/models/analytics.dart';
import 'package:file_picker/file_picker.dart';
import 'package:intl/intl.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

/// Builds and exports the two analytics PDF reports required by the project.
class AnalyticsPdfReports {
  AnalyticsPdfReports._();

  static final _dateFmt = DateFormat('yyyy-MM-dd HH:mm');
  static final _moneyFmt = NumberFormat.currency(symbol: '\$', decimalDigits: 2);

  static Future<Uint8List> buildMoviePerformanceReport(
    List<MoviePerformance> movies,
  ) async {
    final doc = pw.Document();
    final generatedAt = DateTime.now();

    final totalTickets = movies.fold<int>(0, (s, m) => s + m.ticketsSold);
    final totalRevenue = movies.fold<num>(0, (s, m) => s + m.revenue);

    doc.addPage(
      pw.MultiPage(
        pageFormat: PdfPageFormat.a4,
        margin: const pw.EdgeInsets.all(32),
        header: (context) => _header(
          'CineVision — Movie Performance Report',
          generatedAt,
        ),
        footer: (context) => _footer(context),
        build: (context) => [
          pw.SizedBox(height: 8),
          pw.Text(
            'Sales, occupancy and ratings per movie.',
            style: const pw.TextStyle(fontSize: 11, color: PdfColors.grey700),
          ),
          pw.SizedBox(height: 16),
          _summaryBox([
            ('Movies', '${movies.length}'),
            ('Tickets sold', '$totalTickets'),
            ('Total revenue', _moneyFmt.format(totalRevenue)),
          ]),
          pw.SizedBox(height: 20),
          pw.TableHelper.fromTextArray(
            headers: const [
              'Movie',
              'Screenings',
              'Tickets',
              'Revenue',
              'Occupancy',
              'Avg rating',
            ],
            data: movies
                .map(
                  (m) => [
                    m.title,
                    '${m.screeningsCount}',
                    '${m.ticketsSold}',
                    _moneyFmt.format(m.revenue),
                    '${m.occupancyPercent.toStringAsFixed(1)}%',
                    m.avgRating?.toStringAsFixed(1) ?? '—',
                  ],
                )
                .toList(),
            headerStyle: pw.TextStyle(
              fontWeight: pw.FontWeight.bold,
              color: PdfColors.white,
              fontSize: 10,
            ),
            headerDecoration: const pw.BoxDecoration(color: PdfColors.red800),
            cellStyle: const pw.TextStyle(fontSize: 9),
            cellAlignment: pw.Alignment.centerLeft,
            cellAlignments: {
              1: pw.Alignment.center,
              2: pw.Alignment.center,
              3: pw.Alignment.centerRight,
              4: pw.Alignment.centerRight,
              5: pw.Alignment.center,
            },
            border: pw.TableBorder.all(color: PdfColors.grey400, width: 0.4),
            headerCount: 1,
          ),
          if (movies.isEmpty)
            pw.Padding(
              padding: const pw.EdgeInsets.only(top: 24),
              child: pw.Text('No movie performance data available.'),
            ),
        ],
      ),
    );

    return doc.save();
  }

  static Future<Uint8List> buildHallUtilizationReport(
    List<HallUtilization> halls,
    List<TimeSlotPerformance> timeSlots,
  ) async {
    final doc = pw.Document();
    final generatedAt = DateTime.now();

    final totalShows = halls.fold<int>(0, (s, h) => s + h.showCount);
    final totalSeatsSold = halls.fold<int>(0, (s, h) => s + h.seatsSold);

    doc.addPage(
      pw.MultiPage(
        pageFormat: PdfPageFormat.a4,
        margin: const pw.EdgeInsets.all(32),
        header: (context) => _header(
          'CineVision — Hall Utilization Report',
          generatedAt,
        ),
        footer: (context) => _footer(context),
        build: (context) => [
          pw.SizedBox(height: 8),
          pw.Text(
            'Hall usage distribution and performance by daily time slot.',
            style: const pw.TextStyle(fontSize: 11, color: PdfColors.grey700),
          ),
          pw.SizedBox(height: 16),
          _summaryBox([
            ('Halls', '${halls.length}'),
            ('Total shows', '$totalShows'),
            ('Seats sold', '$totalSeatsSold'),
          ]),
          pw.SizedBox(height: 20),
          pw.Text(
            'Hall utilization',
            style: pw.TextStyle(fontSize: 13, fontWeight: pw.FontWeight.bold),
          ),
          pw.SizedBox(height: 8),
          pw.TableHelper.fromTextArray(
            headers: const [
              'Hall',
              'Capacity',
              'Shows',
              'Share',
              'Seats sold',
              'Utilization',
            ],
            data: halls
                .map(
                  (h) => [
                    h.hallName,
                    '${h.capacity}',
                    '${h.showCount}',
                    '${h.sharePercent.toStringAsFixed(1)}%',
                    '${h.seatsSold}',
                    '${h.utilizationPercent.toStringAsFixed(1)}%',
                  ],
                )
                .toList(),
            headerStyle: pw.TextStyle(
              fontWeight: pw.FontWeight.bold,
              color: PdfColors.white,
              fontSize: 10,
            ),
            headerDecoration: const pw.BoxDecoration(color: PdfColors.blueGrey800),
            cellStyle: const pw.TextStyle(fontSize: 9),
            cellAlignments: {
              1: pw.Alignment.center,
              2: pw.Alignment.center,
              3: pw.Alignment.centerRight,
              4: pw.Alignment.center,
              5: pw.Alignment.centerRight,
            },
            border: pw.TableBorder.all(color: PdfColors.grey400, width: 0.4),
          ),
          pw.SizedBox(height: 24),
          pw.Text(
            'Performance by time slot',
            style: pw.TextStyle(fontSize: 13, fontWeight: pw.FontWeight.bold),
          ),
          pw.SizedBox(height: 8),
          pw.TableHelper.fromTextArray(
            headers: const [
              'Time slot',
              'Tickets sold',
              'Occupancy',
              'Revenue',
            ],
            data: timeSlots
                .map(
                  (s) => [
                    s.timeSlot,
                    '${s.ticketsSold}',
                    '${s.occupancyPercent.toStringAsFixed(1)}%',
                    _moneyFmt.format(s.revenue),
                  ],
                )
                .toList(),
            headerStyle: pw.TextStyle(
              fontWeight: pw.FontWeight.bold,
              color: PdfColors.white,
              fontSize: 10,
            ),
            headerDecoration: const pw.BoxDecoration(color: PdfColors.blueGrey800),
            cellStyle: const pw.TextStyle(fontSize: 9),
            cellAlignments: {
              1: pw.Alignment.center,
              2: pw.Alignment.centerRight,
              3: pw.Alignment.centerRight,
            },
            border: pw.TableBorder.all(color: PdfColors.grey400, width: 0.4),
          ),
          if (halls.isEmpty && timeSlots.isEmpty)
            pw.Padding(
              padding: const pw.EdgeInsets.only(top: 24),
              child: pw.Text('No hall / time-slot data available.'),
            ),
        ],
      ),
    );

    return doc.save();
  }

  static Future<void> printPdf(Uint8List bytes, {required String name}) {
    return Printing.layoutPdf(
      onLayout: (_) async => bytes,
      name: name,
    );
  }

  /// Opens a Save As dialog and writes the PDF bytes to disk.
  static Future<String?> downloadPdf(
    Uint8List bytes, {
    required String suggestedFileName,
  }) async {
    final path = await FilePicker.saveFile(
      dialogTitle: 'Save PDF report',
      fileName: suggestedFileName,
      type: FileType.custom,
      allowedExtensions: const ['pdf'],
      bytes: bytes,
    );
    return path;
  }

  static pw.Widget _header(String title, DateTime generatedAt) {
    return pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Row(
          mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
          children: [
            pw.Text(
              title,
              style: pw.TextStyle(
                fontSize: 16,
                fontWeight: pw.FontWeight.bold,
              ),
            ),
            pw.Text(
              'Generated: ${_dateFmt.format(generatedAt)}',
              style: const pw.TextStyle(fontSize: 9, color: PdfColors.grey600),
            ),
          ],
        ),
        pw.SizedBox(height: 6),
        pw.Divider(thickness: 1.2, color: PdfColors.grey400),
      ],
    );
  }

  static pw.Widget _footer(pw.Context context) {
    return pw.Container(
      alignment: pw.Alignment.centerRight,
      margin: const pw.EdgeInsets.only(top: 8),
      child: pw.Text(
        'Page ${context.pageNumber} of ${context.pagesCount}',
        style: const pw.TextStyle(fontSize: 9, color: PdfColors.grey600),
      ),
    );
  }

  static pw.Widget _summaryBox(List<(String, String)> items) {
    return pw.Container(
      padding: const pw.EdgeInsets.all(12),
      decoration: pw.BoxDecoration(
        color: PdfColors.grey100,
        borderRadius: pw.BorderRadius.circular(6),
        border: pw.Border.all(color: PdfColors.grey300),
      ),
      child: pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceAround,
        children: items
            .map(
              (item) => pw.Column(
                children: [
                  pw.Text(
                    item.$2,
                    style: pw.TextStyle(
                      fontSize: 14,
                      fontWeight: pw.FontWeight.bold,
                    ),
                  ),
                  pw.SizedBox(height: 2),
                  pw.Text(
                    item.$1,
                    style: const pw.TextStyle(
                      fontSize: 9,
                      color: PdfColors.grey700,
                    ),
                  ),
                ],
              ),
            )
            .toList(),
      ),
    );
  }
}
