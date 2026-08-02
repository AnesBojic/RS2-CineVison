import 'package:json_annotation/json_annotation.dart';

/// API contract: every instant is UTC. Convert with [DateTime.toLocal] only for display.
class UtcDateTime {
  UtcDateTime._();

  static DateTime now() => DateTime.now().toUtc();

  /// Parse an API instant as UTC. Values without an offset are treated as UTC
  /// (same rule as the backend UtcDateTimeConverter).
  static DateTime? tryParse(dynamic value) {
    if (value == null) return null;
    final raw = value.toString().trim();
    if (raw.isEmpty) return null;

    final parsed = DateTime.tryParse(raw);
    if (parsed == null) return null;
    if (parsed.isUtc) return parsed;

    return DateTime.utc(
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

  static String? toApi(DateTime? value) =>
      value?.toUtc().toIso8601String();
}

/// For `@JsonSerializable` models (e.g. [User]).
class UtcDateTimeJsonConverter implements JsonConverter<DateTime?, Object?> {
  const UtcDateTimeJsonConverter();

  @override
  DateTime? fromJson(Object? json) => UtcDateTime.tryParse(json);

  @override
  Object? toJson(DateTime? object) => UtcDateTime.toApi(object);
}
