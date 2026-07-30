import 'package:ecommerce_mobile/models/news.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class NewsProvider extends BaseProvider<NewsItem> {
  NewsProvider() : super('News');

  @override
  NewsItem fromJson(data) =>
      NewsItem.fromJson(Map<String, dynamic>.from(data as Map));
}
