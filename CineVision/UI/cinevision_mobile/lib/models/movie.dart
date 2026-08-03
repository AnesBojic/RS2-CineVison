import '../core/enums/api_enums.dart';
import '../core/utils/utc_datetime.dart';
import 'genre.dart';

export '../core/enums/api_enums.dart' show MovieState;

class Movie {
  final int? id;
  final String? title;
  final String? description;
  final int? durationMinutes;
  final int? genreId;
  final String? director;
  final DateTime? releaseDate;
  final String? language;
  final String? ageRating;
  final String? trailerUrl;
  final String? posterImageBase64;
  final bool? isActive;
  final int? viewCount;
  final DateTime? createdAt;
  final DateTime? updatedAt;
  final String? movieState;
  final Genre? genre;

  Movie({
    this.id,
    this.title,
    this.description,
    this.durationMinutes,
    this.genreId,
    this.director,
    this.releaseDate,
    this.language,
    this.ageRating,
    this.trailerUrl,
    this.posterImageBase64,
    this.isActive,
    this.viewCount,
    this.createdAt,
    this.updatedAt,
    this.movieState,
    this.genre,
  });

  bool get isActiveState => MovieState.isActive(movieState);

  Movie withoutPoster() => Movie(
        id: id,
        title: title,
        description: description,
        durationMinutes: durationMinutes,
        genreId: genreId,
        director: director,
        releaseDate: releaseDate,
        language: language,
        ageRating: ageRating,
        trailerUrl: trailerUrl,
        posterImageBase64: null,
        isActive: isActive,
        viewCount: viewCount,
        createdAt: createdAt,
        updatedAt: updatedAt,
        movieState: movieState,
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
      director: json['director'] as String?,
      releaseDate: _parseDateOnly(json['releaseDate']),
      language: json['language'] as String?,
      ageRating: json['ageRating'] as String?,
      trailerUrl: json['trailerUrl'] as String?,
      posterImageBase64: json['posterImageBase64'] as String?,
      isActive: json['isActive'] as bool?,
      viewCount: json['viewCount'] as int?,
      createdAt: UtcDateTime.tryParse(json['createdAt']),
      updatedAt: UtcDateTime.tryParse(json['updatedAt']),
      movieState: json['movieState'] as String?,
      genre: json['genre'] != null
          ? Genre.fromJson(json['genre'] as Map<String, dynamic>)
          : null,
    );
  }
}
