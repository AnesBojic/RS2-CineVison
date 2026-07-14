import 'dart:convert';

import 'package:ecommerce_mobile/models/movie.dart';
import 'package:ecommerce_mobile/models/recommendation.dart';
import 'package:ecommerce_mobile/models/search_result.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class MovieProvider extends BaseProvider<Movie> {
  MovieProvider() : super('Movies');

  @override
  Movie fromJson(data) => Movie.fromJson(data);

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

  Future<Movie> getWithPoster(int id) => getById(id);

  /// Hybrid popularity + content-based scores for ranking (take=0 → all active movies).
  Future<List<Recommendation>> getRecommendations({int take = 0}) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/Recommendations')
        .replace(queryParameters: {'take': take.toString()});
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    final data = jsonDecode(response.body);
    if (data is! List) {
      return [];
    }
    return data
        .map((e) => Recommendation.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> registerView(int id) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/View');
    final response = await http.post(uri, headers: createHeaders());
    validateResponse(response);
  }
}
