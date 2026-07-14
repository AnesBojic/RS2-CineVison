import 'dart:convert';

import 'package:ecommerce_mobile/models/review.dart';
import 'package:ecommerce_mobile/models/review_eligibility.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super('Reviews');

  @override
  Review fromJson(data) => Review.fromJson(data as Map<String, dynamic>);

  Future<List<ReviewEligibility>> fetchMyEligibility() async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/MyEligibility');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    final data = jsonDecode(response.body);
    if (data is! List) return [];
    return data
        .map((e) => ReviewEligibility.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<Review> submitReview({
    required int movieId,
    required int rating,
    String? comment,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'movieId': movieId,
        'rating': rating,
        if (comment != null && comment.trim().isNotEmpty)
          'comment': comment.trim(),
      }),
    );
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<Review> updateReview({
    required int reviewId,
    required int rating,
    String? comment,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$reviewId');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'rating': rating,
        if (comment != null && comment.trim().isNotEmpty)
          'comment': comment.trim(),
      }),
    );
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<Review> getReview(int id) => getById(id);
}
