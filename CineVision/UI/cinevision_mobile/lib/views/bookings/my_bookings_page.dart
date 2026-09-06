import 'package:cinevision_mobile/core/widgets/cine_app_bar.dart';
import 'package:cinevision_mobile/core/constants/app_colors.dart';
import 'package:cinevision_mobile/core/constants/app_defaults.dart';
import 'package:cinevision_mobile/core/enums/api_enums.dart';
import 'package:cinevision_mobile/core/routes/app_routes.dart';
import 'package:cinevision_mobile/core/utils/field_validators.dart';
import 'package:cinevision_mobile/models/reservation.dart';
import 'package:cinevision_mobile/models/review_eligibility.dart';
import 'package:cinevision_mobile/providers/auth_provider.dart';
import 'package:cinevision_mobile/providers/reservation_provider.dart';
import 'package:cinevision_mobile/providers/review_provider.dart';
import 'package:cinevision_mobile/utils/api_client_exception.dart';
import 'package:cinevision_mobile/utils/utils_widgets.dart';
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
  int? _statusFilter;

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
        reservationProvider.fetchMyReservations(status: _statusFilter),
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
    final isPaid = reservation.wasPaid;
    final reason = await _askCancelReason(isPaid: isPaid);
    if (reason == null || !mounted) return;

    setState(() => _refundingId = reservation.id);
    try {
      await context.read<ReservationProvider>().cancel(
        reservation.id,
        reason: reason,
      );
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

  Future<String?> _askCancelReason({required bool isPaid}) {
    return showDialog<String>(
      context: context,
      builder: (ctx) => _CancelReasonDialog(isPaid: isPaid),
    );
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
          if (isLoggedIn)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppDefaults.padding,
                12,
                AppDefaults.padding,
                0,
              ),
              child: DropdownButtonFormField<int?>(
                key: ValueKey(_statusFilter),
                initialValue: _statusFilter,
                decoration: const InputDecoration(
                  hintText: 'All statuses',
                ),
                items: const [
                  DropdownMenuItem<int?>(
                    value: null,
                    child: Text('All statuses'),
                  ),
                  DropdownMenuItem<int?>(
                    value: ReservationStatus.pending,
                    child: Text('Pending'),
                  ),
                  DropdownMenuItem<int?>(
                    value: ReservationStatus.confirmed,
                    child: Text('Confirmed'),
                  ),
                  DropdownMenuItem<int?>(
                    value: ReservationStatus.paid,
                    child: Text('Paid'),
                  ),
                  DropdownMenuItem<int?>(
                    value: ReservationStatus.cancelled,
                    child: Text('Cancelled'),
                  ),
                  DropdownMenuItem<int?>(
                    value: ReservationStatus.completed,
                    child: Text('Completed'),
                  ),
                ],
                onChanged: (value) {
                  if (value == _statusFilter) return;
                  setState(() => _statusFilter = value);
                  _load();
                },
              ),
            ),
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
                        ? Center(
                            child: Text(
                              _statusFilter == null
                                  ? 'No bookings yet'
                                  : 'No bookings with this status',
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                              ),
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

/// Owns the reason controller so it is disposed only after the dialog leaves the tree.
class _CancelReasonDialog extends StatefulWidget {
  const _CancelReasonDialog({required this.isPaid});

  final bool isPaid;

  @override
  State<_CancelReasonDialog> createState() => _CancelReasonDialogState();
}

class _CancelReasonDialogState extends State<_CancelReasonDialog> {
  final _reasonCtrl = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _reasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isPaid = widget.isPaid;
    return AlertDialog(
      title: Text(isPaid ? 'Refund ticket?' : 'Cancel booking?'),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              isPaid
                  ? 'Your payment will be refunded and the seats will become available again.'
                  : 'This booking will be cancelled and the seats will become available again.',
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _reasonCtrl,
              maxLines: 3,
              maxLength: 500,
              autofocus: true,
              decoration: const InputDecoration(
                labelText: 'Reason',
                hintText: 'Why are you cancelling this booking?',
              ),
              validator: (v) => FieldValidators.minLength(
                v,
                5,
                field: 'Cancellation reason',
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Keep ticket'),
        ),
        ElevatedButton(
          onPressed: () {
            if (!(_formKey.currentState?.validate() ?? false)) return;
            Navigator.pop(context, _reasonCtrl.text.trim());
          },
          child: Text(isPaid ? 'Refund' : 'Cancel booking'),
        ),
      ],
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
    if (!reservation.isReviewableBooking || !reservation.isProjectionPast) {
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
            formatDateTime(reservation.projectionStartTime),
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
          if (reservation.refundNotice != null) ...[
            const SizedBox(height: 6),
            Text(
              reservation.refundNotice!,
              style: TextStyle(
                fontSize: 12,
                color: reservation.refundStatus == RefundStatus.failed
                    ? AppColors.primary
                    : AppColors.textSecondary,
              ),
            ),
          ],
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
                              reservation.wasPaid
                                  ? Icons.currency_exchange
                                  : Icons.cancel_outlined,
                              size: 18,
                            ),
                      label: Text(
                        reservation.wasPaid ? 'Refund' : 'Cancel',
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ),
              ],
            ),
          ] else if (reservation.isReviewableBooking &&
              !reservation.isProjectionPast) ...[
            const SizedBox(height: 12),
            const Text(
              'Review available after the projection ends',
              style: TextStyle(color: AppColors.textSecondary, fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }
}
