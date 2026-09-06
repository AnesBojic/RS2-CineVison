import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';

class RoleProvider extends BaseProvider<LookupItem> {
  RoleProvider() : super('Roles');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
