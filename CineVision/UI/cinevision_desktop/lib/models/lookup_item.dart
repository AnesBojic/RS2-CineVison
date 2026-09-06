/// Shared shape for reference tables (screen types, hall statuses, age ratings, languages, roles).
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

  /// Roles only.
  final String? color;
  final bool canAccessDesktop;
  final bool canManageUsers;
  final bool canManageMovies;
  final bool canManageHalls;
  final bool canManageProjections;
  final bool canManageNews;
  final bool canManageReferenceData;
  final bool canManageRoles;
  final bool canViewAnalytics;
  final bool canUseChatBot;
  final bool isSystemRole;
  final bool permissionsLocked;

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
    this.color,
    this.canAccessDesktop = false,
    this.canManageUsers = false,
    this.canManageMovies = false,
    this.canManageHalls = false,
    this.canManageProjections = false,
    this.canManageNews = false,
    this.canManageReferenceData = false,
    this.canManageRoles = false,
    this.canViewAnalytics = false,
    this.canUseChatBot = false,
    this.isSystemRole = false,
    this.permissionsLocked = false,
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
      color: json['color'] as String?,
      canAccessDesktop: json['canAccessDesktop'] as bool? ?? false,
      canManageUsers: json['canManageUsers'] as bool? ?? false,
      canManageMovies: json['canManageMovies'] as bool? ?? false,
      canManageHalls: json['canManageHalls'] as bool? ?? false,
      canManageProjections: json['canManageProjections'] as bool? ?? false,
      canManageNews: json['canManageNews'] as bool? ?? false,
      canManageReferenceData: json['canManageReferenceData'] as bool? ?? false,
      canManageRoles: json['canManageRoles'] as bool? ?? false,
      canViewAnalytics: json['canViewAnalytics'] as bool? ?? false,
      canUseChatBot: json['canUseChatBot'] as bool? ?? false,
      isSystemRole: json['isSystemRole'] as bool? ?? false,
      permissionsLocked: json['permissionsLocked'] as bool? ?? false,
    );
  }

  /// Only the fields the edited reference table actually has are sent.
  Map<String, dynamic> toJson({bool includeRoleAccess = false}) => {
        'name': name,
        'description': description ?? '',
        if (allowsProjections != null) 'allowsProjections': allowsProjections,
        if (minimumAge != null) 'minimumAge': minimumAge,
        if (code != null) 'code': code,
        if (includeRoleAccess) ...{
          'color': color,
          'canAccessDesktop': canAccessDesktop,
          'canManageUsers': canManageUsers,
          'canManageMovies': canManageMovies,
          'canManageHalls': canManageHalls,
          'canManageProjections': canManageProjections,
          'canManageNews': canManageNews,
          'canManageReferenceData': canManageReferenceData,
          'canManageRoles': canManageRoles,
          'canViewAnalytics': canViewAnalytics,
          'canUseChatBot': canUseChatBot,
        },
      };
}
