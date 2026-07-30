import 'genre.dart';

class MovieState {
  static const active = 'ActiveMovieState';
  static const draft = 'DraftMovieState';

  static String displayLabel(String? movieState) {
    if (movieState == null || movieState.isEmpty) return 'Draft';
    if (movieState.toLowerCase().contains('active')) return 'Active';
    if (movieState.toLowerCase().contains('draft')) return 'Draft';
    return movieState;
  }

  static bool isActive(String? movieState) =>
      movieState != null && movieState.toLowerCase().contains('active');

  static String filterValueForLabel(String label) {
    switch (label) {
      case 'Active':
        return active;
      case 'Draft':
        return draft;
      default:
        return label;
    }
  }
}

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

  String get displayState => MovieState.displayLabel(movieState);

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

  /// A release date is a calendar day, not an instant, so it must not shift with
  /// the viewer's timezone. The API stores it as UTC midnight; read and write the
  /// UTC calendar components so the day stays the same everywhere.
  static DateTime? _parseDateOnly(dynamic value) {
    final parsed = value == null ? null : DateTime.tryParse(value.toString());
    if (parsed == null) return null;
    final utc = parsed.toUtc();
    return DateTime(utc.year, utc.month, utc.day);
  }

  static String? _toDateOnlyApi(DateTime? value) => value == null
      ? null
      : DateTime.utc(value.year, value.month, value.day).toIso8601String();

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
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString())
          : null,
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString())
          : null,
      movieState: json['movieState'] as String?,
      genre: json['genre'] != null
          ? Genre.fromJson(json['genre'] as Map<String, dynamic>)
          : null,
    );
  }

  /// Metadata only — poster is uploaded separately via PUT /Movies/{id}/Poster.
  Map<String, dynamic> toInsertJson() => {
        'title': title,
        'description': description ?? '',
        'durationMinutes': durationMinutes ?? 0,
        'genreId': genreId,
        'director': director,
        'releaseDate': _toDateOnlyApi(releaseDate),
        'language': language,
        'ageRating': ageRating,
        'trailerUrl': trailerUrl,
        'isActive': true,
      };

  /// Never send poster in update — avoids wiping or re-uploading huge payloads.
  Map<String, dynamic> toUpdateJson() => toInsertJson();
}
