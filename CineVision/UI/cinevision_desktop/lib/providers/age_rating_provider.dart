import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';

class AgeRatingProvider extends BaseProvider<LookupItem> {
  AgeRatingProvider() : super('AgeRatings');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
