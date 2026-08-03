import 'package:cinevision_mobile/core/enums/api_enums.dart';
import 'package:cinevision_mobile/core/widgets/cine_app_bar.dart';
import 'package:cinevision_mobile/core/utils/date_formatters.dart';
import 'package:cinevision_mobile/core/constants/app_colors.dart';
import 'package:cinevision_mobile/core/constants/app_defaults.dart';
import 'package:cinevision_mobile/core/routes/app_routes.dart';
import 'package:cinevision_mobile/models/reservation.dart';
import 'package:flutter/material.dart';

class BookingConfirmedPage extends StatelessWidget {
  const BookingConfirmedPage({
    super.key,
    required this.reservation,
    this.genreLine,
  });

  final Reservation reservation;
  final String? genreLine;

  String _formatDate(DateTime dt) => DateFormatters.longWeekdayDate(dt);

  String _formatTime(DateTime dt) => DateFormatters.timeHm(dt);

  int get _seatCount {
    return reservation.seats.fold<int>(
      0,
      (sum, s) => sum + SeatTypes.spotsOccupied(s.seatType),
    );
  }

  @override
  Widget build(BuildContext context) {
    final start = reservation.screeningStartTime.toLocal();
    final subtitle = genreLine ?? '';

    return Scaffold(
      appBar: const CineAppBar(
        title: 'Booking Confirmed',
        showBack: true,
      ),
      body: SingleChildScrollView(
          padding: const EdgeInsets.all(AppDefaults.padding),
          child: Column(
            children: [
              const SizedBox(height: 24),
              Container(
                width: 72,
                height: 72,
                decoration: const BoxDecoration(
                  color: AppColors.success,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.check, color: Colors.white, size: 40),
              ),
              const SizedBox(height: 16),
              Text(
                'Booking Confirmed!',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Your tickets have been successfully purchased',
                style: TextStyle(color: AppColors.textSecondary),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              Container(
                clipBehavior: Clip.antiAlias,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(16),
                  color: AppColors.cardColor,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(AppDefaults.padding),
                      color: AppColors.cardHeader,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            reservation.movieTitle,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 18,
                            ),
                          ),
                          if (subtitle.isNotEmpty) ...[
                            const SizedBox(height: 4),
                            Text(
                              subtitle,
                              style: const TextStyle(
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    Padding(
                      padding: const EdgeInsets.all(AppDefaults.padding),
                      child: Column(
                        children: [
                          _DetailRow(
                            icon: Icons.person_outline,
                            label: 'Guest',
                            value: reservation.customerName ?? 'Guest',
                          ),
                          _DetailRow(
                            icon: Icons.calendar_today,
                            label: 'Date',
                            value: _formatDate(start),
                          ),
                          _DetailRow(
                            icon: Icons.access_time,
                            label: 'Time',
                            value: _formatTime(start),
                          ),
                          _DetailRow(
                            icon: Icons.location_on_outlined,
                            label: 'Theater',
                            value: reservation.hallName,
                          ),
                          _DetailRow(
                            icon: Icons.confirmation_number_outlined,
                            label: 'Seats',
                            value: '$_seatCount Seat${_seatCount == 1 ? '' : 's'}',
                          ),
                        ],
                      ),
                    ),
                    const Divider(height: 1),
                    Padding(
                      padding: const EdgeInsets.all(AppDefaults.padding),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Total Paid',
                            style: TextStyle(color: AppColors.textSecondary),
                          ),
                          Text(
                            '\$${reservation.totalAmount.toStringAsFixed(2)}',
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 20,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(AppDefaults.padding),
                decoration: BoxDecoration(
                  color: AppColors.cardColor,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Text(
                  'A confirmation email has been sent with your ticket details. Please arrive 15 minutes before showtime.',
                  style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
                  textAlign: TextAlign.center,
                ),
              ),
              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () {
                    Navigator.pushNamedAndRemoveUntil(
                      context,
                      AppRoutes.entryPoint,
                      (_) => false,
                    );
                  },
                  child: const Text('Browse More Movies'),
                ),
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton(
                  onPressed: () {
                    Navigator.pushNamedAndRemoveUntil(
                      context,
                      AppRoutes.myBookings,
                      (route) => route.settings.name == AppRoutes.entryPoint,
                    );
                  },
                  child: const Text('View My Bookings'),
                ),
              ),
            ],
          ),
        ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Icon(icon, color: AppColors.textSecondary, size: 20),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                  ),
                ),
                Text(
                  value,
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
