import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/models/review_eligibility.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/providers/review_provider.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
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
  int? _refundingId;

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
      final reservationProvider = context.read<ReservationProvider>();
      final reviewProvider = context.read<ReviewProvider>();
      // Bookings and review eligibility are independent feeds.
      final loaded = await Future.wait([
        reservationProvider.fetchMyReservations(),
        reviewProvider.fetchMyEligibility(),
      ]);
      final reservations = loaded[0] as List<Reservation>;
      final eligibility = loaded[1] as List<ReviewEligibility>;
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

  Future<void> _refund(Reservation reservation) async {
    final isPaid = reservation.isPaid;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isPaid ? 'Refund ticket?' : 'Cancel booking?'),
        content: Text(
          isPaid
              ? 'Your payment will be refunded and the seats will become available again.'
              : 'This booking will be cancelled and the seats will become available again.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Keep ticket'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isPaid ? 'Refund' : 'Cancel booking'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _refundingId = reservation.id);
    try {
      await context.read<ReservationProvider>().cancel(reservation.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isPaid
                ? 'Ticket refunded. Seats are available again.'
                : 'Booking cancelled. Seats are available again.',
          ),
        ),
      );
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Refund failed', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Refund failed', e.toString());
    } finally {
      if (mounted) setState(() => _refundingId = null);
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
                                  onRefund: () => _refund(r),
                                  isRefunding: _refundingId == r.id,
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
    required this.onRefund,
    required this.isRefunding,
  });

  final Reservation reservation;
  final ReviewEligibility? eligibility;
  final String Function(DateTime) formatDateTime;
  final VoidCallback onReview;
  final VoidCallback onRefund;
  final bool isRefunding;

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
          if (reservation.canRefund || _showReviewButton) ...[
            const SizedBox(height: 12),
            Row(
              children: [
                if (_showReviewButton)
                  Expanded(
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
                            ? 'Edit review'
                            : 'Write review',
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
                if (_showReviewButton && reservation.canRefund)
                  const SizedBox(width: 8),
                if (reservation.canRefund)
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: isRefunding ? null : onRefund,
                      icon: isRefunding
                          ? const SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Icon(
                              reservation.isPaid
                                  ? Icons.currency_exchange
                                  : Icons.cancel_outlined,
                              size: 18,
                            ),
                      label: Text(
                        reservation.isPaid ? 'Refund' : 'Cancel',
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
              ],
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
