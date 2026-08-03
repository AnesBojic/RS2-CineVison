import '../core/utils/utc_datetime.dart';

class Review {
  final int? id;
  final int movieId;
  final String movieTitle;
  final int userId;
  final String userName;
  final int rating;
  final String? comment;
  final DateTime? createdAt;

  Review({
    this.id,
    required this.movieId,
    required this.movieTitle,
    required this.userId,
    required this.userName,
    required this.rating,
    this.comment,
    this.createdAt,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: json['id'] as int?,
      movieId: json['movieId'] as int? ?? 0,
      movieTitle: json['movieTitle'] as String? ?? '',
      userId: json['userId'] as int? ?? 0,
      userName: json['userName'] as String? ?? '',
      rating: json['rating'] as int? ?? 0,
      comment: json['comment'] as String?,
      createdAt: UtcDateTime.tryParse(json['createdAt']),
    );
  }
}
