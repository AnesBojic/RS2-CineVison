import '../core/utils/utc_datetime.dart';

class NewsItem {
  final int? id;
  final String? title;
  final String? content;
  final DateTime? publishedAt;
  final bool? isActive;

  NewsItem({
    this.id,
    this.title,
    this.content,
    this.publishedAt,
    this.isActive,
  });

  factory NewsItem.fromJson(Map<String, dynamic> json) {
    return NewsItem(
      id: json['id'] as int?,
      title: json['title'] as String?,
      content: json['content'] as String?,
      publishedAt: UtcDateTime.tryParse(json['publishedAt']),
      isActive: json['isActive'] as bool?,
    );
  }
}
