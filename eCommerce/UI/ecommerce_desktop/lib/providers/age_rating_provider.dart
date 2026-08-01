import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class AgeRatingProvider extends BaseProvider<LookupItem> {
  AgeRatingProvider() : super('AgeRatings');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
