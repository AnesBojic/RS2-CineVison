import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/models/review_eligibility.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class MyBookingsPage extends StatefulWidget {
  const MyBookingsPage({super.key});

  @override
  State<MyBookingsPage> createState() => _MyBookingsPageState();
}

class _MyBookingsPageState extends State<MyBookingsPage> {
  List<Reservation> _reservations = [];
  Map<int, ReviewEligibility> _eligibilityByMovie = {};
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {
      setState(() {
        _loading = false;
        _reservations = [];
        _eligibilityByMovie = {};
      });
      return;
    }

    setState(() => _loading = true);
    try {
      final reservations =
          await context.read<ReservationProvider>().fetchMyReservations();
      final eligibility =
          await context.read<ReviewProvider>().fetchMyEligibility();
      if (!mounted) return;
      setState(() {
        _reservations = reservations;
        _eligibilityByMovie = {
          for (final e in eligibility) e.movieId: e,
        };
        _loading = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _openReview(Reservation reservation) async {
    final eligibility = _eligibilityByMovie[reservation.movieId];
    if (eligibility == null) return;

    final result = await Navigator.pushNamed(
      context,
      AppRoutes.submitReview,
      arguments: {
        'movieId': reservation.movieId,
        'movieTitle': reservation.movieTitle,
        if (eligibility.hasReview) 'reviewId': eligibility.existingReviewId,
      },
    );

    if (result == true) {
      _load();
    }
  }

  String _formatDateTime(DateTime dt) {
    final local = dt.toLocal();
    return '${local.day}/${local.month}/${local.year} ${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final isLoggedIn =
        AuthProvider.accesstoken != null && AuthProvider.accesstoken!.isNotEmpty;

    return Scaffold(
      appBar: const CineAppBar(title: 'My Bookings', showBack: true),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: !isLoggedIn
                ? _LoginPrompt(
                    onLogin: () async {
                      await Navigator.pushNamed(context, AppRoutes.login);
                      _load();
                    },
                  )
                : _loading
                    ? const Center(child: CircularProgressIndicator())
                    : _reservations.isEmpty
                        ? const Center(
                            child: Text(
                              'No bookings yet',
                              style: TextStyle(color: AppColors.textSecondary),
                            ),
                          )
                        : RefreshIndicator(
                            onRefresh: _load,
                            child: ListView.separated(
                              padding:
                                  const EdgeInsets.all(AppDefaults.padding),
                              itemCount: _reservations.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 12),
                              itemBuilder: (_, index) {
                                final r = _reservations[index];
                                return _BookingCard(
                                  reservation: r,
                                  eligibility: _eligibilityByMovie[r.movieId],
                                  formatDateTime: _formatDateTime,
                                  onReview: () => _openReview(r),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }
}

class _LoginPrompt extends StatelessWidget {
  const _LoginPrompt({required this.onLogin});

  final VoidCallback onLogin;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppDefaults.padding),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.confirmation_number_outlined,
              size: 64,
              color: AppColors.textSecondary,
            ),
            const SizedBox(height: 16),
            const Text(
              'Sign in to view your bookings',
              style: TextStyle(color: AppColors.textSecondary),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: onLogin,
              child: const Text('Sign in'),
            ),
          ],
        ),
      ),
    );
  }
}

class _BookingCard extends StatelessWidget {
  const _BookingCard({
    required this.reservation,
    required this.eligibility,
    required this.formatDateTime,
    required this.onReview,
  });

  final Reservation reservation;
  final ReviewEligibility? eligibility;
  final String Function(DateTime) formatDateTime;
  final VoidCallback onReview;

  bool get _showReviewButton {
    if (!reservation.isPaidOrConfirmed || !reservation.isScreeningPast) {
      return false;
    }
    if (eligibility == null) return false;
    return eligibility!.canReview || eligibility!.hasReview;
  }

  @override
  Widget build(BuildContext context) {
    final seatLabels = reservation.seats
        .map((s) => '${s.rowLabel}${s.seatNumber}')
        .join(', ');

    return Container(
      padding: const EdgeInsets.all(AppDefaults.padding),
      decoration: BoxDecoration(
        color: AppColors.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            reservation.movieTitle,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
          ),
          const SizedBox(height: 8),
          Text(
            formatDateTime(reservation.screeningStartTime),
            style: const TextStyle(color: AppColors.textSecondary),
          ),
          const SizedBox(height: 4),
          Text(
            reservation.hallName,
            style: const TextStyle(color: AppColors.textSecondary),
          ),
          if (seatLabels.isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              'Seats: $seatLabels',
              style: const TextStyle(color: AppColors.textSecondary),
            ),
          ],
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.gray,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  reservation.statusName,
                  style: const TextStyle(fontSize: 12),
                ),
              ),
              Text(
                '\$${reservation.totalAmount.toStringAsFixed(2)}',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
            ],
          ),
          if (_showReviewButton) ...[
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: onReview,
                icon: Icon(
                  eligibility?.hasReview == true
                      ? Icons.rate_review_outlined
                      : Icons.star_outline,
                  size: 18,
                ),
                label: Text(
                  eligibility?.hasReview == true
                      ? 'Edit your review'
                      : 'Write a review',
                ),
              ),
            ),
          ] else if (reservation.isPaidOrConfirmed &&
              !reservation.isScreeningPast) ...[
            const SizedBox(height: 12),
            const Text(
              'Review available after the screening ends',
              style: TextStyle(color: AppColors.textSecondary, fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }
}
