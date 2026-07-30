class Screening {
  final int? id;
  final int? movieId;
  final String? movieTitle;
  final String? moviePosterBase64;
  final int? hallId;
  final String? hallName;
  final DateTime? startTime;
  final DateTime? endTime;
  final num? basePrice;
  final String? language;
  final bool? hasSubtitles;
  final bool? isActive;
  final int? totalSeats;
  final int? availableSeats;

  Screening({
    this.id,
    this.movieId,
    this.movieTitle,
    this.moviePosterBase64,
    this.hallId,
    this.hallName,
    this.startTime,
    this.endTime,
    this.basePrice,
    this.language,
    this.hasSubtitles,
    this.isActive,
    this.totalSeats,
    this.availableSeats,
  });

  /// The API always sends UTC (trailing `Z`); the UI works in local time.
  static DateTime? _parseUtcAsLocal(dynamic value) {
    final parsed = value == null ? null : DateTime.tryParse(value.toString());
    return parsed?.toLocal();
  }

  factory Screening.fromJson(Map<String, dynamic> json) {
    final movie = json['movie'];
    return Screening(
      id: json['id'] as int?,
      movieId: json['movieId'] as int?,
      movieTitle: json['movieTitle'] as String?,
      moviePosterBase64: movie is Map
          ? movie['posterImageBase64'] as String?
          : null,
      hallId: json['hallId'] as int?,
      hallName: json['hallName'] as String?,
      startTime: _parseUtcAsLocal(json['startTime']),
      endTime: _parseUtcAsLocal(json['endTime']),
      basePrice: json['basePrice'] as num?,
      language: json['language'] as String?,
      hasSubtitles: json['hasSubtitles'] as bool?,
      isActive: json['isActive'] as bool?,
      totalSeats: json['totalSeats'] as int?,
      availableSeats: json['availableSeats'] as int?,
    );
  }
}
