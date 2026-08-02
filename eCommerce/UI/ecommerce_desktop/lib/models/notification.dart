import '../core/utils/utc_datetime.dart';

class AppNotification {
  final int? id;
  final String? title;
  final String? message;
  final String? type;
  final bool? isRead;
  final DateTime? createdAt;

  AppNotification({
    this.id,
    this.title,
    this.message,
    this.type,
    this.isRead,
    this.createdAt,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: json['id'] as int?,
      title: json['title'] as String?,
      message: json['message'] as String?,
      type: json['type'] as String?,
      isRead: json['isRead'] as bool?,
      createdAt: UtcDateTime.tryParse(json['createdAt']),
    );
  }
}
