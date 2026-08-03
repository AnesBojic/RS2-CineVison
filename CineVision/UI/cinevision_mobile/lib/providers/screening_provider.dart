import 'dart:convert';

import 'package:cinevision_mobile/models/screening.dart';
import 'package:cinevision_mobile/models/screening_seat.dart';
import 'package:cinevision_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ScreeningProvider extends BaseProvider<Screening> {
  ScreeningProvider() : super('Screenings');

  @override
  Screening fromJson(data) => Screening.fromJson(data);

  Future<List<ScreeningSeat>> getSeats(int screeningId) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Screenings/$screeningId/Seats');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    final data = jsonDecode(response.body) as List<dynamic>;
    return data
        .map((e) => ScreeningSeat.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
