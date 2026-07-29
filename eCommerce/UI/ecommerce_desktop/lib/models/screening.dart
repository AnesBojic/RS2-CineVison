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

  /// Keep the clock-face numbers the user typed / the API stored.
  /// Do not convert UTC↔local here — that caused the persistent 2h shift.
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

  /// Serialize as local wall-clock without a `Z` suffix so the API stores
  /// exactly the hour/minute chosen in the desktop picker.
  static String? _toWallClockApi(DateTime? value) {
    if (value == null) return null;
    final local = value.isUtc ? value.toLocal() : value;
    final wall = DateTime(
      local.year,
      local.month,
      local.day,
      local.hour,
      local.minute,
      local.second,
    );
    final y = wall.year.toString().padLeft(4, '0');
    final m = wall.month.toString().padLeft(2, '0');
    final d = wall.day.toString().padLeft(2, '0');
    final hh = wall.hour.toString().padLeft(2, '0');
    final mm = wall.minute.toString().padLeft(2, '0');
    final ss = wall.second.toString().padLeft(2, '0');
    return '$y-$m-${d}T$hh:$mm:$ss';
  }

  factory Screening.fromJson(Map<String, dynamic> json) {
    final movie = json['movie'];
    final nestedPoster =
        movie is Map ? movie['posterImageBase64'] as String? : null;
    return Screening(
      id: json['id'] as int?,
      movieId: json['movieId'] as int?,
      movieTitle: json['movieTitle'] as String?,
      moviePosterBase64: (json['moviePosterBase64'] as String?) ?? nestedPoster,
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

  Map<String, dynamic> toJson() => {
        'movieId': movieId,
        'hallId': hallId,
        'startTime': _toWallClockApi(startTime),
        'basePrice': basePrice,
        'language': language,
        'hasSubtitles': hasSubtitles ?? false,
        'isActive': isActive ?? true,
      };
}
