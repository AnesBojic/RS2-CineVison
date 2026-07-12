import 'dart:convert';

import 'package:ecommerce_desktop/models/hall.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class HallProvider extends BaseProvider<Hall> {
  HallProvider() : super('Halls');

  @override
  Hall fromJson(data) => Hall.fromJson(data);

  Future<Hall> updateSeatLayout(int hallId, List<Map<String, dynamic>> seats) async {
    final url = '${BaseProvider.baseUrl}$endpoint/$hallId/SeatLayout';
    final uri = Uri.parse(url);
    final headers = createHeaders();
    final body = jsonEncode({'seats': seats});
    final response = await http.put(uri, headers: headers, body: body);
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }
}
