import 'dart:convert';

import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReservationProvider extends BaseProvider<Reservation> {
  ReservationProvider() : super('Reservations');

  @override
  Reservation fromJson(data) => Reservation.fromJson(data);

  Future<List<Reservation>> fetchMyReservations() async {
    final result = await get(filter: {'page': 1, 'pageSize': 100});
    return result.items ?? [];
  }

  Future<Map<String, String>> createPaymentIntent({
    required int screeningId,
    required List<int> seatIds,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Reservations/CreatePaymentIntent');
    final headers = createHeaders();
    final body = jsonEncode({
      'screeningId': screeningId,
      'seatIds': seatIds,
    });
    final response = await http.post(uri, headers: headers, body: body);
    validateResponse(response);
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final clientSecret = data['clientSecret'] as String;
    final paymentIntentId = (data['paymentIntentId'] as String?) ??
        clientSecret.split('_secret_').first;
    return {
      'paymentIntentId': paymentIntentId,
      'clientSecret': clientSecret,
      'publishableKey': data['publishableKey'] as String,
    };
  }

  Future<Reservation> reserve({
    required int screeningId,
    required List<int> seatIds,
    String? paymentIntentId,
    String? customerName,
    String? customerEmail,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Reservations/Reserve');
    final headers = createHeaders();
    final body = jsonEncode({
      'screeningId': screeningId,
      'seatIds': seatIds,
      if (paymentIntentId != null) 'paymentIntentId': paymentIntentId,
      if (customerName != null && customerName.isNotEmpty)
        'customerName': customerName,
      if (customerEmail != null && customerEmail.isNotEmpty)
        'customerEmail': customerEmail,
    });
    final response = await http.post(uri, headers: headers, body: body);
    validateResponse(response);
    return Reservation.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Reservation> cancel(int reservationId) async {
    final uri =
        Uri.parse('${BaseProvider.baseUrl}Reservations/$reservationId/Cancel');
    final response = await http.post(uri, headers: createHeaders());
    validateResponse(response);
    return Reservation.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
