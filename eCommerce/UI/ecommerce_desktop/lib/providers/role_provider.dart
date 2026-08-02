import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

/// Read-only roles for the user form (seeded; no CRUD screen).
class RoleProvider extends BaseProvider<LookupItem> {
  RoleProvider() : super('Roles');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
