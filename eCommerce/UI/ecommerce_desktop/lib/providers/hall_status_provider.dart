import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class HallStatusProvider extends BaseProvider<LookupItem> {
  HallStatusProvider() : super('HallStatuses');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
