/// Shared shape for reference tables (screen types, hall statuses, age ratings, languages).
class LookupItem {
  final int? id;
  final String? name;
  final String? description;

  final int inUseCount;
  final bool canDelete;
  final String? deleteBlockedReason;

  /// Hall statuses only.
  final bool? allowsProjections;

  /// Age ratings only.
  final int? minimumAge;

  /// Languages only.
  final String? code;

  LookupItem({
    this.id,
    this.name,
    this.description,
    this.inUseCount = 0,
    this.canDelete = true,
    this.deleteBlockedReason,
    this.allowsProjections,
    this.minimumAge,
    this.code,
  });

  factory LookupItem.fromJson(Map<String, dynamic> json) {
    return LookupItem(
      id: json['id'] as int?,
      name: json['name'] as String?,
      description: json['description'] as String?,
      inUseCount: json['inUseCount'] as int? ?? 0,
      canDelete: json['canDelete'] as bool? ?? true,
      deleteBlockedReason: json['deleteBlockedReason'] as String?,
      allowsProjections: json['allowsProjections'] as bool?,
      minimumAge: json['minimumAge'] as int?,
      code: json['code'] as String?,
    );
  }

  /// Only the fields the edited reference table actually has are sent.
  Map<String, dynamic> toJson() => {
        'name': name,
        'description': description ?? '',
        if (allowsProjections != null) 'allowsProjections': allowsProjections,
        if (minimumAge != null) 'minimumAge': minimumAge,
        if (code != null) 'code': code,
      };
}
