import '../core/utils/utc_datetime.dart';

class NewsItem {
  final int? id;
  final String? title;
  final String? content;
  final DateTime? publishedAt;
  final bool? isActive;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  NewsItem({
    this.id,
    this.title,
    this.content,
    this.publishedAt,
    this.isActive,
    this.createdAt,
    this.updatedAt,
  });

  factory NewsItem.fromJson(Map<String, dynamic> json) {
    return NewsItem(
      id: json['id'] as int?,
      title: json['title'] as String?,
      content: json['content'] as String?,
      publishedAt: UtcDateTime.tryParse(json['publishedAt']),
      isActive: json['isActive'] as bool?,
      createdAt: UtcDateTime.tryParse(json['createdAt']),
      updatedAt: UtcDateTime.tryParse(json['updatedAt']),
    );
  }

  Map<String, dynamic> toInsertJson() => {
        'title': title,
        'content': content,
        'publishedAt': UtcDateTime.toApi(publishedAt),
        'isActive': isActive ?? true,
      };

  Map<String, dynamic> toUpdateJson() => toInsertJson();
}
