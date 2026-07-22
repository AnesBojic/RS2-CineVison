import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/core/utils/checkout_platform.dart';
import 'package:ecommerce_mobile/core/utils/date_formatters.dart';
import 'package:ecommerce_mobile/core/components/base64_image.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/booking_provider.dart';
import 'package:ecommerce_mobile/providers/reservation_provider.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:provider/provider.dart';

/// Step 5 — checkout with customer info and payment (mockup c6).
class CheckoutPage extends StatefulWidget {
  const CheckoutPage({super.key});

  @override
  State<CheckoutPage> createState() => _CheckoutPageState();
}

class _CheckoutPageState extends State<CheckoutPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _cardController = TextEditingController();
  final _expiryController = TextEditingController();
  final _cvvController = TextEditingController();
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _prefillFromToken();
  }

  void _prefillFromToken() {
    final claims = AuthProvider.accessTokenDecoded;
    if (claims == null) return;

    final first = claims['FirstName']?.toString() ?? '';
    final last = claims['LastName']?.toString() ?? '';
    final name = '$first $last'.trim();
    if (name.isNotEmpty) {
      _nameController.text = name;
    }

    final email = claims['Email']?.toString();
    if (email != null && email.isNotEmpty) {
      _emailController.text = email;
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _cardController.dispose();
    _expiryController.dispose();
    _cvvController.dispose();
    super.dispose();
  }

  Future<bool> _ensureLoggedIn() async {
    if (AuthProvider.accesstoken != null && AuthProvider.accesstoken!.isNotEmpty) {
      return true;
    }

    final proceed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Login required'),
        content: const Text(
          'Please sign in or register to complete your booking.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Sign In / Register'),
          ),
        ],
      ),
    );

    if (proceed == true && mounted) {
      await Navigator.pushNamed(context, AppRoutes.authLanding);
      _prefillFromToken();
    }

    return AuthProvider.accesstoken != null && AuthProvider.accesstoken!.isNotEmpty;
  }

  Future<Reservation> _submitReservation({
    required ReservationProvider reservationProvider,
    required int screeningId,
    required List<int> seatIds,
    String? paymentIntentId,
  }) {
    return reservationProvider.reserve(
      screeningId: screeningId,
      seatIds: seatIds,
      paymentIntentId: paymentIntentId,
      customerName: _nameController.text.trim(),
      customerEmail: _emailController.text.trim(),
    );
  }

  Future<void> _completePurchase(BookingProvider booking) async {
    if (!await _ensureLoggedIn()) return;
    if (!(_formKey.currentState?.validate() ?? false)) return;

    final screening = booking.screening;
    if (screening?.id == null || booking.selectedSeatIds.isEmpty) return;

    setState(() => _busy = true);
    try {
      final reservationProvider = context.read<ReservationProvider>();
      final seatIds = booking.selectedSeatIds.toList();
      final screeningId = screening!.id!;

      Reservation reservation;

      if (supportsStripePaymentSheet) {
        try {
          final intentData = await reservationProvider.createPaymentIntent(
            screeningId: screeningId,
            seatIds: seatIds,
          );

          Stripe.publishableKey = intentData['publishableKey']!;

          await Stripe.instance.initPaymentSheet(
            paymentSheetParameters: SetupPaymentSheetParameters(
              paymentIntentClientSecret: intentData['clientSecret']!,
              merchantDisplayName: 'CineVision',
            ),
          );

          await Stripe.instance.presentPaymentSheet();

          final paymentIntentId =
              intentData['clientSecret']!.split('_secret_').first;

          reservation = await _submitReservation(
            reservationProvider: reservationProvider,
            screeningId: screeningId,
            seatIds: seatIds,
            paymentIntentId: paymentIntentId,
          );
        } on StripeException catch (e) {
          final msg =
              e.error.localizedMessage ?? e.error.message ?? 'Payment cancelled.';
          if (mounted) alertBox(context, 'Payment', msg);
          return;
        }
      } else {
        // Demo checkout for Windows / desktop / web (matches mockup disclaimer).
        reservation = await _submitReservation(
          reservationProvider: reservationProvider,
          screeningId: screeningId,
          seatIds: seatIds,
        );
      }

      final genreLine = _genreSubtitle(booking);
      booking.reset();

      if (!mounted) return;
      Navigator.pushNamedAndRemoveUntil(
        context,
        AppRoutes.bookingConfirmed,
        (route) => route.settings.name == AppRoutes.entryPoint,
        arguments: {
          'reservation': reservation,
          'genreLine': genreLine,
        },
      );
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Booking failed', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Checkout', e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  String? _genreSubtitle(BookingProvider booking) {
    final movie = booking.movie;
    if (movie == null) return null;
    final genre = movie.genre?.name ?? '';
    final duration = movie.durationMinutes;
    if (genre.isEmpty && duration == null) return null;
    if (duration != null && genre.isNotEmpty) return '$genre • $duration min';
    return genre.isNotEmpty ? genre : '$duration min';
  }

  String _formatDate(DateTime dt) => DateFormatters.shortWeekdayDate(dt);

  String _formatTime(DateTime dt) => DateFormatters.timeHm(dt);

  @override
  Widget build(BuildContext context) {
    return Consumer<BookingProvider>(
      builder: (context, booking, _) {
        final movie = booking.movie;
        final screening = booking.screening;
        final total = booking.totalPrice;

        if (movie == null || screening == null) {
          return Scaffold(
            appBar: const CineAppBar(title: 'Checkout', showBack: true),
            body: const Center(child: Text('No booking in progress')),
          );
        }

        return Scaffold(
          appBar: const CineAppBar(title: 'Checkout', showBack: true),
          bottomNavigationBar: SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(AppDefaults.padding),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: _busy ? null : () => _completePurchase(booking),
                      child: _busy
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Text(
                              'Complete Purchase - \$${total.toStringAsFixed(2)}',
                            ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    supportsStripePaymentSheet
                        ? 'Payment is processed securely via Stripe.'
                        : 'This is a demo payment form. No actual charges will be made.',
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: AppColors.textSecondary,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
          ),
          body: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(
              AppDefaults.padding,
              AppDefaults.padding,
              AppDefaults.padding,
              120,
            ),
            child: Form(
              key: _formKey,
              child: Column(
                children: [
                  _SummaryCard(
                    movie: movie,
                    screening: screening,
                    seatCount: booking.selectedSeatCount,
                    total: total,
                    formatDate: _formatDate,
                    formatTime: _formatTime,
                  ),
                  const SizedBox(height: 16),
                  if (AuthProvider.accesstoken == null ||
                      AuthProvider.accesstoken!.isEmpty)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: OutlinedButton.icon(
                        onPressed: () async {
                          await Navigator.pushNamed(
                            context,
                            AppRoutes.authLanding,
                          );
                          if (mounted) {
                            setState(_prefillFromToken);
                          }
                        },
                        icon: const Icon(Icons.person_outline),
                        label: const Text('Sign In / Register'),
                      ),
                    ),
                  _FormCard(
                    title: 'Customer Information',
                    child: Column(
                      children: [
                        TextFormField(
                          controller: _nameController,
                          decoration: const InputDecoration(
                            labelText: 'Full Name',
                            hintText: 'John Doe',
                          ),
                          validator: (v) =>
                              (v == null || v.trim().isEmpty) ? 'Required' : null,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _emailController,
                          decoration: const InputDecoration(
                            labelText: 'Email',
                            hintText: 'john@example.com',
                          ),
                          keyboardType: TextInputType.emailAddress,
                          validator: (v) {
                            if (v == null || v.trim().isEmpty) return 'Required';
                            if (!v.contains('@')) return 'Enter a valid email';
                            return null;
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (!supportsStripePaymentSheet)
                    _FormCard(
                      title: 'Payment Information',
                      icon: Icons.credit_card_outlined,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          TextFormField(
                            controller: _cardController,
                            decoration: const InputDecoration(
                              labelText: 'Card number',
                              hintText: '1234 5678 9012 3456',
                            ),
                            keyboardType: TextInputType.number,
                          ),
                          const SizedBox(height: 12),
                          Row(
                            children: [
                              Expanded(
                                child: TextFormField(
                                  controller: _expiryController,
                                  decoration: const InputDecoration(
                                    labelText: 'Expiry Date',
                                    hintText: 'MM/YY',
                                  ),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: TextFormField(
                                  controller: _cvvController,
                                  decoration: const InputDecoration(
                                    labelText: 'CVV',
                                    hintText: '123',
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({
    required this.movie,
    required this.screening,
    required this.seatCount,
    required this.total,
    required this.formatDate,
    required this.formatTime,
  });

  final dynamic movie;
  final dynamic screening;
  final int seatCount;
  final num total;
  final String Function(DateTime) formatDate;
  final String Function(DateTime) formatTime;

  @override
  Widget build(BuildContext context) {
    return _FormCard(
      title: 'Booking Summary',
      child: Column(
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: SizedBox(
                  width: 64,
                  height: 90,
                  child: movie.posterImageBase64 != null
                      ? Base64ImageWithLoader(movie.posterImageBase64!)
                      : Container(color: AppColors.gray),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      movie.title ?? '',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    _InfoRow(
                      icon: Icons.calendar_today,
                      text: screening.startTime != null
                          ? formatDate(screening.startTime!.toLocal())
                          : '',
                    ),
                    _InfoRow(
                      icon: Icons.access_time,
                      text: screening.startTime != null
                          ? formatTime(screening.startTime!.toLocal())
                          : '',
                    ),
                    _InfoRow(
                      icon: Icons.location_on_outlined,
                      text: screening.hallName ?? '',
                    ),
                    _InfoRow(
                      icon: Icons.confirmation_number_outlined,
                      text: '$seatCount Seat${seatCount == 1 ? '' : 's'}',
                    ),
                  ],
                ),
              ),
            ],
          ),
          const Divider(height: 24),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Total',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
              ),
              Text(
                '\$${total.toStringAsFixed(2)}',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        children: [
          Icon(icon, size: 14, color: AppColors.textSecondary),
          const SizedBox(width: 6),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(
                color: AppColors.textSecondary,
                fontSize: 13,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FormCard extends StatelessWidget {
  const _FormCard({
    required this.title,
    required this.child,
    this.icon,
  });

  final String title;
  final Widget child;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppDefaults.padding),
      decoration: BoxDecoration(
        color: AppColors.cardColor,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (icon != null) ...[
                Icon(icon, size: 20),
                const SizedBox(width: 8),
              ],
              Text(
                title,
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          child,
        ],
      ),
    );
  }
}
