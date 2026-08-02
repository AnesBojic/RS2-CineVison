import 'package:ecommerce_mobile/core/components/base64_image.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/core/utils/date_formatters.dart';
import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/models/movie.dart';
import 'package:ecommerce_mobile/models/screening.dart';
import 'package:ecommerce_mobile/models/screening_seat.dart';
import 'package:ecommerce_mobile/models/search_result.dart';
import 'package:ecommerce_mobile/providers/booking_provider.dart';
import 'package:ecommerce_mobile/providers/movie_provider.dart';
import 'package:ecommerce_mobile/providers/screening_provider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Steps 2–4 — pick date, showtime, then seats (mockups c3–c5).
class BookingPage extends StatefulWidget {
  const BookingPage({super.key, required this.movie});

  final Movie movie;

  @override
  State<BookingPage> createState() => _BookingPageState();
}

class _BookingPageState extends State<BookingPage> {
  late ScreeningProvider _screeningProvider;
  late BookingProvider _bookingProvider;

  Movie? _movie;
  List<Screening> _screenings = [];
  DateTime? _selectedDate;
  bool _loadingScreenings = true;
  bool _loadingSeats = false;

  @override
  void initState() {
    super.initState();
    _screeningProvider = context.read<ScreeningProvider>();
    _bookingProvider = context.read<BookingProvider>();
    _movie = widget.movie;
    _bookingProvider.startBooking(widget.movie);
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _loadingScreenings = true);
    try {
      final needsPoster =
          (_movie?.posterImageBase64 ?? '').isEmpty && _movie?.id != null;
      final movieProvider = context.read<MovieProvider>();

      // Poster and upcoming projections are independent — fetch them together.
      final loaded = await Future.wait([
        _screeningProvider.get(
          filter: {
            'movieId': widget.movie.id,
            'onlyUpcoming': true,
            'includeMovie': true,
            'includeHall': true,
            'pageSize': 200,
          },
        ),
        if (needsPoster)
          movieProvider.getWithPoster(_movie!.id!)
        else
          Future.value(_movie),
      ]);

      final result = loaded[0] as SearchResult<Screening>;
      if (needsPoster) {
        _movie = loaded[1] as Movie;
        _bookingProvider.startBooking(_movie!);
      }

      final items = (result.items ?? [])
          .where((s) => s.isActive != false && s.startTime != null)
          .toList()
        ..sort(
          (a, b) => a.startTime!.toLocal().compareTo(b.startTime!.toLocal()),
        );

      DateTime? firstDate;
      if (items.isNotEmpty) {
        final local = items.first.startTime!.toLocal();
        firstDate = DateFormatters.dateOnly(local);
      }

      if (!mounted) return;
      setState(() {
        _screenings = items;
        _selectedDate = firstDate;
        _loadingScreenings = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _loadingScreenings = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  List<DateTime> get _availableDates {
    final dates = <DateTime>{};
    for (final s in _screenings) {
      if (s.startTime == null) continue;
      dates.add(DateFormatters.dateOnly(s.startTime!.toLocal()));
    }
    return dates.toList()..sort();
  }

  List<Screening> get _dayScreenings {
    if (_selectedDate == null) return [];
    return _screenings.where((s) {
      if (s.startTime == null) return false;
      return DateFormatters.dateOnly(s.startTime!.toLocal()) == _selectedDate;
    }).toList();
  }

  Map<String, List<Screening>> get _groupedShowtimes {
    final map = <String, List<Screening>>{};
    for (final s in _dayScreenings) {
      final key = DateFormatters.timeOfDayCategory(s.startTime!.toLocal());
      map.putIfAbsent(key, () => []).add(s);
    }
    return map;
  }

  Future<void> _onScreeningSelected(Screening screening) async {
    _bookingProvider.selectScreening(screening);
    setState(() => _loadingSeats = true);
    try {
      final seats = await _screeningProvider.getSeats(screening.id!);
      if (!mounted) return;
      _bookingProvider.setSeats(seats);
      setState(() => _loadingSeats = false);
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _loadingSeats = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  void _onDateSelected(DateTime date) {
    setState(() {
      _selectedDate = date;
    });
    _bookingProvider.selectScreening(null);
  }

  @override
  Widget build(BuildContext context) {
    final movie = _movie ?? widget.movie;

    return Consumer<BookingProvider>(
      builder: (context, booking, _) {
        final grouped = _groupedShowtimes;
        final selectedId = booking.screening?.id;
        final seatCount = booking.selectedSeatCount;

        return Scaffold(
          appBar: CineAppBar(
            title: movie.title ?? '',
            showBack: true,
          ),
          bottomNavigationBar: selectedId != null
              ? SafeArea(
                  child: Padding(
                    padding: const EdgeInsets.all(AppDefaults.padding),
                    child: ElevatedButton(
                      onPressed: seatCount > 0
                          ? () => Navigator.pushNamed(
                                context,
                                AppRoutes.checkout,
                              )
                          : null,
                      child: Text(
                        seatCount > 0
                            ? 'Checkout ($seatCount seat${seatCount == 1 ? '' : 's'})'
                            : 'Select seats to checkout',
                      ),
                    ),
                  ),
                )
              : null,
          body: _loadingScreenings
          ? const Center(child: CircularProgressIndicator())
          : _screenings.isEmpty
              ? const Center(child: Text('No upcoming showtimes'))
              : SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(
                    AppDefaults.padding,
                    AppDefaults.padding,
                    AppDefaults.padding + 10,
                    100,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          ClipRRect(
                            borderRadius: BorderRadius.circular(12),
                            child: SizedBox(
                              width: 100,
                              height: 140,
                              child: movie.posterImageBase64 != null &&
                                      movie.posterImageBase64!.isNotEmpty
                                  ? Base64ImageWithLoader(
                                      movie.posterImageBase64!,
                                    )
                                  : Container(
                                      color: AppColors.cardColor,
                                      child: const Icon(Icons.movie),
                                    ),
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Select Date:',
                                  style: TextStyle(fontWeight: FontWeight.bold),
                                ),
                                const SizedBox(height: 8),
                                SingleChildScrollView(
                                  scrollDirection: Axis.horizontal,
                                  child: Row(
                                    children: _availableDates.map((date) {
                                      final selected = _selectedDate == date;
                                      return Padding(
                                        padding: const EdgeInsets.only(right: 8),
                                        child: GestureDetector(
                                          onTap: () => _onDateSelected(date),
                                          child: Container(
                                            width: 72,
                                            padding: const EdgeInsets.symmetric(
                                              vertical: 12,
                                            ),
                                            decoration: BoxDecoration(
                                              color: selected
                                                  ? AppColors.primary
                                                  : AppColors.cardColor,
                                              borderRadius:
                                                  BorderRadius.circular(12),
                                            ),
                                            child: Column(
                                              children: [
                                                Text(DateFormatters.dayLabel(date)),
                                                Text(
                                                  '${date.day}',
                                                  style: const TextStyle(
                                                    fontWeight: FontWeight.bold,
                                                    fontSize: 18,
                                                  ),
                                                ),
                                                if (selected)
                                                  const Text(
                                                    'Selected',
                                                    style: TextStyle(fontSize: 10),
                                                  ),
                                              ],
                                            ),
                                          ),
                                        ),
                                      );
                                    }).toList(),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Text(
                        'Showtimes:',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                        ),
                      ),
                      const SizedBox(height: 12),
                      if (_dayScreenings.isEmpty)
                        const Text(
                          'No showtimes on this date',
                          style: TextStyle(color: AppColors.textSecondary),
                        )
                      else
                        ...grouped.entries.expand((entry) {
                          return [
                            Text(
                              entry.key,
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                              ),
                            ),
                            const SizedBox(height: 8),
                            if (entry.key == 'Evening')
                              Wrap(
                                spacing: 8,
                                runSpacing: 8,
                                children: entry.value.map((s) {
                                  return SizedBox(
                                    width:
                                        (MediaQuery.of(context).size.width -
                                                AppDefaults.padding * 2 -
                                                8) /
                                            2,
                                    child: _ShowtimeButton(
                                      screening: s,
                                      selected: selectedId == s.id,
                                      onTap: () => _onScreeningSelected(s),
                                    ),
                                  );
                                }).toList(),
                              )
                            else
                              ...entry.value.map(
                                (s) => Padding(
                                  padding: const EdgeInsets.only(bottom: 8),
                                  child: _ShowtimeButton(
                                    screening: s,
                                    selected: selectedId == s.id,
                                    onTap: () => _onScreeningSelected(s),
                                    fullWidth: true,
                                  ),
                                ),
                              ),
                            const SizedBox(height: 8),
                          ];
                        }),
                      if (selectedId != null) ...[
                        const SizedBox(height: 8),
                        _SeatSelectionSection(loading: _loadingSeats),
                      ],
                    ],
                  ),
                ),
        );
      },
    );
  }
}

class _ShowtimeButton extends StatelessWidget {
  const _ShowtimeButton({
    required this.screening,
    required this.selected,
    required this.onTap,
    this.fullWidth = false,
  });

  final Screening screening;
  final bool selected;
  final VoidCallback onTap;
  final bool fullWidth;

  @override
  Widget build(BuildContext context) {
    final local = screening.startTime!.toLocal();
    final label =
        '${DateFormatters.timeHm(local)} - ${screening.hallName ?? 'Theater'}';

    final child = GestureDetector(
      onTap: onTap,
      child: Container(
        width: fullWidth ? double.infinity : null,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : AppColors.cardColor,
          borderRadius: BorderRadius.circular(12),
        ),
        alignment: Alignment.center,
        child: Text(label, textAlign: TextAlign.center),
      ),
    );

    return fullWidth ? child : child;
  }
}

class _SeatSelectionSection extends StatelessWidget {
  const _SeatSelectionSection({required this.loading});

  final bool loading;

  @override
  Widget build(BuildContext context) {
    return Consumer<BookingProvider>(
      builder: (context, booking, _) {
        if (loading) {
          return const Padding(
            padding: EdgeInsets.all(24),
            child: Center(child: CircularProgressIndicator()),
          );
        }

        if (booking.seats.isEmpty) return const SizedBox.shrink();

        final rows = <String, List<ScreeningSeat>>{};
        for (final seat in booking.seats) {
          if (booking.isPartnerSlot(seat)) continue;
          rows.putIfAbsent(seat.rowLabel, () => []).add(seat);
        }
        for (final list in rows.values) {
          list.sort((a, b) => a.seatNumber.compareTo(b.seatNumber));
        }
        final rowLabels = rows.keys.toList()..sort();

        return Container(
          padding: const EdgeInsets.all(AppDefaults.padding),
          decoration: BoxDecoration(
            color: AppColors.cardColor,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: AppColors.separator),
          ),
          child: Column(
            children: [
              const Text(
                'Select Your Seats',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
              ),
              const SizedBox(height: 16),
              Container(
                height: 6,
                margin: const EdgeInsets.symmetric(horizontal: 24),
                decoration: BoxDecoration(
                  color: AppColors.gray,
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'SCREEN',
                style: TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 11,
                  letterSpacing: 2,
                ),
              ),
              const SizedBox(height: 20),
              // One shared horizontal scroll for the whole map (no per-row bars).
              ScrollConfiguration(
                behavior: ScrollConfiguration.of(context).copyWith(
                  scrollbars: false,
                ),
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: rowLabels.map((row) {
                      final rowSeats = rows[row]!;
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [
                            SizedBox(
                              width: 20,
                              child: Text(
                                row,
                                style: const TextStyle(
                                  color: AppColors.textSecondary,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            ...rowSeats.map((seat) {
                              return Padding(
                                padding: const EdgeInsets.only(right: 6),
                                child: _SeatWidget(seat: seat),
                              );
                            }),
                          ],
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Wrap(
                alignment: WrapAlignment.center,
                spacing: 16,
                runSpacing: 8,
                children: const [
                  _LegendItem(color: AppColors.seatAvailable, label: 'Available'),
                  _LegendItem(color: AppColors.primary, label: 'Selected'),
                  _LegendItem(color: AppColors.seatOccupied, label: 'Occupied'),
                  _LegendItem(
                    color: AppColors.seatAvailable,
                    label: 'Couple Seats',
                    icon: Icons.favorite,
                    wide: true,
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                'Selected: ${booking.selectedSeatCount} seat${booking.selectedSeatCount == 1 ? '' : 's'}',
                style: const TextStyle(color: AppColors.textSecondary),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _SeatWidget extends StatelessWidget {
  const _SeatWidget({required this.seat});

  final ScreeningSeat seat;

  @override
  Widget build(BuildContext context) {
    return Consumer<BookingProvider>(
      builder: (context, booking, _) {
        final selected = booking.selectedSeatIds.contains(seat.seatId);
        Color bg;
        if (seat.isTaken) {
          bg = AppColors.seatOccupied;
        } else if (selected) {
          bg = AppColors.primary;
        } else {
          bg = AppColors.seatAvailable;
        }

        final width = seat.isCouple ? 52.0 : 28.0;

        return GestureDetector(
          onTap: seat.isTaken ? null : () => booking.toggleSeat(seat),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            width: width,
            height: 28,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: bg,
              borderRadius: BorderRadius.circular(6),
            ),
            child: seat.isCouple
                ? const Icon(Icons.favorite, size: 14, color: Colors.white)
                : Text(
                    '${seat.seatNumber}',
                    style: TextStyle(
                      fontSize: 11,
                      color: selected ? Colors.white : AppColors.textSecondary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
          ),
        );
      },
    );
  }
}

class _LegendItem extends StatelessWidget {
  const _LegendItem({
    required this.color,
    required this.label,
    this.icon,
    this.wide = false,
  });

  final Color color;
  final String label;
  final IconData? icon;
  final bool wide;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: wide ? 28 : 16,
          height: 16,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(4),
          ),
          child: icon != null
              ? Icon(icon, size: 10, color: AppColors.textSecondary)
              : null,
        ),
        const SizedBox(width: 6),
        Text(
          label,
          style: const TextStyle(color: AppColors.textSecondary, fontSize: 12),
        ),
      ],
    );
  }
}
