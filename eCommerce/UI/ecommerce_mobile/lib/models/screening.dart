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

  /// Keep API clock-face numbers as local wall-clock (no UTC shift).
  static DateTime? _parseWallClock(dynamic value) {
    if (value == null) return null;
    final parsed = DateTime.tryParse(value.toString());
    if (parsed == null) return null;
    return DateTime(
      parsed.year,
      parsed.month,
      parsed.day,
      parsed.hour,
      parsed.minute,
      parsed.second,
      parsed.millisecond,
      parsed.microsecond,
    );
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
      startTime: _parseWallClock(json['startTime']),
      endTime: _parseWallClock(json['endTime']),
      basePrice: json['basePrice'] as num?,
      language: json['language'] as String?,
      hasSubtitles: json['hasSubtitles'] as bool?,
      isActive: json['isActive'] as bool?,
      totalSeats: json['totalSeats'] as int?,
      availableSeats: json['availableSeats'] as int?,
    );
  }
}
