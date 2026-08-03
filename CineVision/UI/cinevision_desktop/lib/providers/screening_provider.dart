import 'dart:convert';

import 'package:cinevision_desktop/models/screening.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ScreeningProvider extends BaseProvider<Screening> {
  ScreeningProvider() : super('Screenings');

  @override
  Screening fromJson(data) => Screening.fromJson(data);

  Future<Map<String, dynamic>> getDeleteImpact(int id) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Screenings/$id/DeleteImpact');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    return jsonDecode(response.body) as Map<String, dynamic>;
  }
}
