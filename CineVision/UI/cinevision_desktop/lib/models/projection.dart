import '../core/utils/utc_datetime.dart';

class Projection {
  final int? id;
  final int? movieId;
  final String? movieTitle;
  final String? moviePosterBase64;
  final int? hallId;
  final String? hallName;
  final DateTime? startTime;
  final DateTime? endTime;
  final num? basePrice;

  /// Reference table id + name for display.
  final int? languageId;
  final String? language;
  final int? totalSeats;
  final int? availableSeats;

  Projection({
    this.id,
    this.movieId,
    this.movieTitle,
    this.moviePosterBase64,
    this.hallId,
    this.hallName,
    this.startTime,
    this.endTime,
    this.basePrice,
    this.languageId,
    this.language,
    this.totalSeats,
    this.availableSeats,
  });

  factory Projection.fromJson(Map<String, dynamic> json) {
    final movie = json['movie'];
    final nestedPoster =
        movie is Map ? movie['posterImageBase64'] as String? : null;
    return Projection(
      id: json['id'] as int?,
      movieId: json['movieId'] as int?,
      movieTitle: json['movieTitle'] as String?,
      moviePosterBase64: (json['moviePosterBase64'] as String?) ?? nestedPoster,
      hallId: json['hallId'] as int?,
      hallName: json['hallName'] as String?,
      startTime: UtcDateTime.tryParse(json['startTime']),
      endTime: UtcDateTime.tryParse(json['endTime']),
      basePrice: json['basePrice'] as num?,
      languageId: json['languageId'] as int?,
      language: json['language'] as String?,
      totalSeats: json['totalSeats'] as int?,
      availableSeats: json['availableSeats'] as int?,
    );
  }

  Map<String, dynamic> toJson() => {
        'movieId': movieId,
        'hallId': hallId,
        'startTime': UtcDateTime.toApi(startTime),
        'basePrice': basePrice,
        'languageId': languageId,
      };
}
