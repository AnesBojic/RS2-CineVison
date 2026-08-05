import 'dart:convert';

import 'package:cinevision_mobile/models/projection.dart';
import 'package:cinevision_mobile/models/projection_seat.dart';
import 'package:cinevision_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ProjectionProvider extends BaseProvider<Projection> {
  ProjectionProvider() : super('Projections');

  @override
  Projection fromJson(data) => Projection.fromJson(data);

  Future<List<ProjectionSeat>> getSeats(int projectionId) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}Projections/$projectionId/Seats');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    final data = jsonDecode(response.body) as List<dynamic>;
    return data
        .map((e) => ProjectionSeat.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
