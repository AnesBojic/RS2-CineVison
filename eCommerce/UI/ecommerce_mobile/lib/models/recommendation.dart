import 'movie.dart';

class Recommendation {
  final Movie movie;
  final double score;
  final double popularityScore;
  final double contentScore;
  final String reason;

  Recommendation({
    required this.movie,
    required this.score,
    required this.popularityScore,
    required this.contentScore,
    required this.reason,
  });

  factory Recommendation.fromJson(Map<String, dynamic> json) {
    return Recommendation(
      movie: Movie.fromJson(json['movie'] as Map<String, dynamic>),
      score: (json['score'] as num?)?.toDouble() ?? 0,
      popularityScore: (json['popularityScore'] as num?)?.toDouble() ?? 0,
      contentScore: (json['contentScore'] as num?)?.toDouble() ?? 0,
      reason: json['reason'] as String? ?? '',
    );
  }
}
