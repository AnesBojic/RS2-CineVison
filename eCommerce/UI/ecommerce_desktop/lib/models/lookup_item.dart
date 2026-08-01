/// A row from one of the reference (lookup) tables: screen types, hall statuses,
/// age ratings or languages. One model covers all four because the API returns the
/// same shape, plus one type-specific field each.
class LookupItem {
  final int? id;
  final String? name;
  final String? description;
  final bool? isActive;

  /// How many records reference this row, and whether it may be deleted.
  final int inUseCount;
  final bool canDelete;
  final String? deleteBlockedReason;

  /// Hall statuses only: halls with this status can host new projections.
  final bool? allowsScreenings;

  /// Age ratings only.
  final int? minimumAge;

  /// Languages only.
  final String? code;

  LookupItem({
    this.id,
    this.name,
    this.description,
    this.isActive,
    this.inUseCount = 0,
    this.canDelete = true,
    this.deleteBlockedReason,
    this.allowsScreenings,
    this.minimumAge,
    this.code,
  });

  factory LookupItem.fromJson(Map<String, dynamic> json) {
    return LookupItem(
      id: json['id'] as int?,
      name: json['name'] as String?,
      description: json['description'] as String?,
      isActive: json['isActive'] as bool?,
      inUseCount: json['inUseCount'] as int? ?? 0,
      canDelete: json['canDelete'] as bool? ?? true,
      deleteBlockedReason: json['deleteBlockedReason'] as String?,
      allowsScreenings: json['allowsScreenings'] as bool?,
      minimumAge: json['minimumAge'] as int?,
      code: json['code'] as String?,
    );
  }

  /// Only the fields the edited reference table actually has are sent.
  Map<String, dynamic> toJson() => {
        'name': name,
        'description': description ?? '',
        'isActive': isActive ?? true,
        if (allowsScreenings != null) 'allowsScreenings': allowsScreenings,
        if (minimumAge != null) 'minimumAge': minimumAge,
        if (code != null) 'code': code,
      };
}
