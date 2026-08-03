class ReviewEligibility {
  final int movieId;
  final String movieTitle;
  final bool canReview;
  final bool hasReview;
  final int? existingReviewId;

  ReviewEligibility({
    required this.movieId,
    required this.movieTitle,
    required this.canReview,
    required this.hasReview,
    this.existingReviewId,
  });

  factory ReviewEligibility.fromJson(Map<String, dynamic> json) {
    return ReviewEligibility(
      movieId: json['movieId'] as int? ?? 0,
      movieTitle: json['movieTitle'] as String? ?? '',
      canReview: json['canReview'] as bool? ?? false,
      hasReview: json['hasReview'] as bool? ?? false,
      existingReviewId: json['existingReviewId'] as int?,
    );
  }
}
