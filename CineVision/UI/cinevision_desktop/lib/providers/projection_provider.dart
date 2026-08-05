import 'dart:convert';

import 'package:cinevision_desktop/models/projection.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ProjectionProvider extends BaseProvider<Projection> {
  ProjectionProvider() : super('Projections');

  @override
  Projection fromJson(data) => Projection.fromJson(data);

  Future<Map<String, dynamic>> getDeleteImpact(int id) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Projections/$id/DeleteImpact');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    return jsonDecode(response.body) as Map<String, dynamic>;
  }
}
