import '../core/utils/utc_datetime.dart';
import 'genre.dart';

class Movie {
  final int? id;
  final String? title;
  final String? description;
  final int? durationMinutes;
  final int? genreId;
  final DateTime? releaseDate;
  final String? language;
  final String? ageRating;
  final String? posterImageBase64;
  final int? viewCount;
  final DateTime? createdAt;
  final DateTime? updatedAt;
  final Genre? genre;

  Movie({
    this.id,
    this.title,
    this.description,
    this.durationMinutes,
    this.genreId,
    this.releaseDate,
    this.language,
    this.ageRating,
    this.posterImageBase64,
    this.viewCount,
    this.createdAt,
    this.updatedAt,
    this.genre,
  });

  Movie withoutPoster() => Movie(
        id: id,
        title: title,
        description: description,
        durationMinutes: durationMinutes,
        genreId: genreId,
        releaseDate: releaseDate,
        language: language,
        ageRating: ageRating,
        posterImageBase64: null,
        viewCount: viewCount,
        createdAt: createdAt,
        updatedAt: updatedAt,
        genre: genre,
      );

  /// Release date is a calendar day (UTC midnight on the API); keep the day stable.
  static DateTime? _parseDateOnly(dynamic value) {
    final utc = UtcDateTime.tryParse(value);
    if (utc == null) return null;
    return DateTime(utc.year, utc.month, utc.day);
  }

  factory Movie.fromJson(Map<String, dynamic> json) {
    return Movie(
      id: json['id'] as int?,
      title: json['title'] as String?,
      description: json['description'] as String?,
      durationMinutes: json['durationMinutes'] as int?,
      genreId: json['genreId'] as int?,
      releaseDate: _parseDateOnly(json['releaseDate']),
      language: json['language'] as String?,
      ageRating: json['ageRating'] as String?,
      posterImageBase64: json['posterImageBase64'] as String?,
      viewCount: json['viewCount'] as int?,
      createdAt: UtcDateTime.tryParse(json['createdAt']),
      updatedAt: UtcDateTime.tryParse(json['updatedAt']),
      genre: json['genre'] != null
          ? Genre.fromJson(json['genre'] as Map<String, dynamic>)
          : null,
    );
  }
}
