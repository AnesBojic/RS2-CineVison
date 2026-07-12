import 'dart:convert';

import 'package:ecommerce_desktop/models/movie.dart';
import 'package:ecommerce_desktop/models/search_result.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class MovieProvider extends BaseProvider<Movie> {
  MovieProvider() : super('Movies');

  @override
  Movie fromJson(data) => Movie.fromJson(data);

  /// List fetch — strips poster base64 by default to keep responses lightweight.
  Future<SearchResult<Movie>> get({
    dynamic filter,
    bool includePoster = false,
  }) async {
    final result = await super.get(filter: filter);
    if (!includePoster && result.items != null) {
      result.items = result.items!.map((m) => m.withoutPoster()).toList();
    }
    return result;
  }

  Future<void> uploadPoster(int id, String base64) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Movies/$id/Poster');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode({'posterImageBase64': base64}),
    );
    validateResponse(response);
  }

  Future<Movie> activate(int id) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Movies/$id/Activate');
    final response = await http.post(uri, headers: createHeaders());
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<Movie> deactivate(int id) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Movies/$id/Deactivate');
    final response = await http.post(uri, headers: createHeaders());
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<Movie> getWithPoster(int id) => getById(id);
}
