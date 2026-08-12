import 'package:cinevision_mobile/core/components/base64_image.dart';
import 'package:cinevision_mobile/core/constants/app_colors.dart';
import 'package:cinevision_mobile/core/constants/app_defaults.dart';
import 'package:cinevision_mobile/core/routes/app_routes.dart';
import 'package:cinevision_mobile/core/utils/date_formatters.dart';
import 'package:cinevision_mobile/core/widgets/cine_app_bar.dart';
import 'package:cinevision_mobile/models/movie.dart';
import 'package:cinevision_mobile/models/review.dart';
import 'package:cinevision_mobile/models/projection.dart';
import 'package:cinevision_mobile/models/projection_seat.dart';
import 'package:cinevision_mobile/models/search_result.dart';
import 'package:cinevision_mobile/providers/booking_provider.dart';
import 'package:cinevision_mobile/providers/movie_provider.dart';
import 'package:cinevision_mobile/providers/review_provider.dart';
import 'package:cinevision_mobile/providers/projection_provider.dart';
import 'package:cinevision_mobile/utils/utils_widgets.dart';
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
  late ProjectionProvider _projectionProvider;
  late BookingProvider _bookingProvider;

  Movie? _movie;
  List<Projection> _projections = [];
  List<Review> _reviews = [];
  DateTime? _selectedDate;
  bool _loadingProjections = true;
  bool _loadingReviews = true;
  bool _loadingSeats = false;

  @override
  void initState() {
    super.initState();
    _projectionProvider = context.read<ProjectionProvider>();
    _bookingProvider = context.read<BookingProvider>();
    _movie = widget.movie;
    _bookingProvider.startBooking(widget.movie);
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _loadingProjections = true;
      _loadingReviews = true;
    });
    try {
      final needsPoster =
          (_movie?.posterImageBase64 ?? '').isEmpty && _movie?.id != null;
      final movieProvider = context.read<MovieProvider>();
      final reviewProvider = context.read<ReviewProvider>();
      final movieId = widget.movie.id;

      final loaded = await Future.wait([
        _projectionProvider.get(
          filter: {
            'movieId': movieId,
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
        if (movieId != null)
          reviewProvider.getForMovie(movieId)
        else
          Future.value(<Review>[]),
      ]);

      final result = loaded[0] as SearchResult<Projection>;
      if (needsPoster) {
        _movie = loaded[1] as Movie;
        _bookingProvider.startBooking(_movie!);
      }
      final reviews = loaded[2] as List<Review>;

      final items = (result.items ?? [])
          .where((s) => s.startTime != null)
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
        _projections = items;
        _reviews = reviews;
        _selectedDate = firstDate;
        _loadingProjections = false;
        _loadingReviews = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() {
        _loadingProjections = false;
        _loadingReviews = false;
      });
      alertBox(context, 'Error', e.toString());
    }
  }

  List<DateTime> get _availableDates {
    final dates = <DateTime>{};
    for (final s in _projections) {
      if (s.startTime == null) continue;
      dates.add(DateFormatters.dateOnly(s.startTime!.toLocal()));
    }
    return dates.toList()..sort();
  }

  List<Projection> get _dayProjections {
    if (_selectedDate == null) return [];
    return _projections.where((s) {
      if (s.startTime == null) return false;
      return DateFormatters.dateOnly(s.startTime!.toLocal()) == _selectedDate;
    }).toList();
  }

  Map<String, List<Projection>> get _groupedShowtimes {
    final map = <String, List<Projection>>{};
    for (final s in _dayProjections) {
      final key = DateFormatters.timeOfDayCategory(s.startTime!.toLocal());
      map.putIfAbsent(key, () => []).add(s);
    }
    return map;
  }

  Future<void> _onProjectionSelected(Projection projection) async {
    _bookingProvider.selectProjection(projection);
    setState(() => _loadingSeats = true);
    try {
      final seats = await _projectionProvider.getSeats(projection.id!);
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
    _bookingProvider.selectProjection(null);
  }

  @override
  Widget build(BuildContext context) {
    final movie = _movie ?? widget.movie;

    return Consumer<BookingProvider>(
      builder: (context, booking, _) {
        final grouped = _groupedShowtimes;
        final selectedId = booking.projection?.id;
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
          body: _loadingProjections
              ? const Center(child: CircularProgressIndicator())
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
                            child: _projections.isEmpty
                                ? const Padding(
                                    padding: EdgeInsets.only(top: 24),
                                    child: Text(
                                      'No upcoming showtimes',
                                      style: TextStyle(
                                        color: AppColors.textSecondary,
                                      ),
                                    ),
                                  )
                                : Column(
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
                      const SizedBox(height: 16),
                      _MovieDetails(movie: movie),
                      if (_projections.isNotEmpty) ...[
                        const SizedBox(height: 24),
                        const Text(
                          'Showtimes:',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 12),
                        if (_dayProjections.isEmpty)
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
                                        projection: s,
                                        selected: selectedId == s.id,
                                        onTap: () => _onProjectionSelected(s),
                                      ),
                                    );
                                  }).toList(),
                                )
                              else
                                ...entry.value.map(
                                  (s) => Padding(
                                    padding: const EdgeInsets.only(bottom: 8),
                                    child: _ShowtimeButton(
                                      projection: s,
                                      selected: selectedId == s.id,
                                      onTap: () => _onProjectionSelected(s),
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
                      const SizedBox(height: 24),
                      _ReviewsSection(
                        loading: _loadingReviews,
                        reviews: _reviews,
                      ),
                    ],
                  ),
                ),
        );
      },
    );
  }
}

class _MovieDetails extends StatelessWidget {
  const _MovieDetails({required this.movie});

  final Movie movie;

  @override
  Widget build(BuildContext context) {
    final meta = [
      if ((movie.genre?.name ?? '').trim().isNotEmpty) movie.genre!.name!.trim(),
      if ((movie.language ?? '').trim().isNotEmpty) movie.language!.trim(),
      if ((movie.durationMinutes ?? 0) > 0) '${movie.durationMinutes} min',
      if ((movie.ageRating ?? '').trim().isNotEmpty) movie.ageRating!.trim(),
    ].join(' · ');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (meta.isNotEmpty)
          Text(
            meta,
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 13,
            ),
          ),
        if ((movie.description ?? '').trim().isNotEmpty) ...[
          const SizedBox(height: 10),
          Text(
            movie.description!.trim(),
            style: const TextStyle(
              color: AppColors.textSecondary,
              height: 1.35,
            ),
            maxLines: 4,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ],
    );
  }
}

class _ReviewsSection extends StatelessWidget {
  const _ReviewsSection({
    required this.loading,
    required this.reviews,
  });

  final bool loading;
  final List<Review> reviews;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Reviews',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
        ),
        const SizedBox(height: 10),
        if (loading)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 12),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (reviews.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: AppColors.cardColor,
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Text(
              'No reviews yet',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          )
        else
          ...reviews.map((review) => _ReviewTile(review: review)),
      ],
    );
  }
}

class _ReviewTile extends StatelessWidget {
  const _ReviewTile({required this.review});

  final Review review;

  @override
  Widget build(BuildContext context) {
    final comment = (review.comment ?? '').trim();
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  review.userName.isEmpty ? 'Customer' : review.userName,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ),
              Row(
                mainAxisSize: MainAxisSize.min,
                children: List.generate(5, (i) {
                  final filled = i < review.rating;
                  return Icon(
                    filled ? Icons.star : Icons.star_border,
                    size: 16,
                    color: filled ? AppColors.primary : AppColors.placeholder,
                  );
                }),
              ),
            ],
          ),
          if (comment.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              comment,
              style: const TextStyle(
                color: AppColors.textSecondary,
                height: 1.35,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _ShowtimeButton extends StatelessWidget {
  const _ShowtimeButton({
    required this.projection,
    required this.selected,
    required this.onTap,
    this.fullWidth = false,
  });

  final Projection projection;
  final bool selected;
  final VoidCallback onTap;
  final bool fullWidth;

  @override
  Widget build(BuildContext context) {
    final local = projection.startTime!.toLocal();
    final label =
        '${DateFormatters.timeHm(local)} - ${projection.hallName ?? 'Theater'}';

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

        final rows = <String, List<ProjectionSeat>>{};
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

  final ProjectionSeat seat;

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
