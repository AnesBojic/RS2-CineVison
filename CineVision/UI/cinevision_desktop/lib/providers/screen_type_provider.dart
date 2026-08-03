import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';

class ScreenTypeProvider extends BaseProvider<LookupItem> {
  ScreenTypeProvider() : super('ScreenTypes');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
