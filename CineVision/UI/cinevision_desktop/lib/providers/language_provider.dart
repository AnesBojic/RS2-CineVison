import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';

class LanguageProvider extends BaseProvider<LookupItem> {
  LanguageProvider() : super('Languages');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
