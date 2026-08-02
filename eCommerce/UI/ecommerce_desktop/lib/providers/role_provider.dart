import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

/// Read-only feed for the role picker in the user form. Roles are seeded with the
/// schema because their names drive authorization, so there is no CRUD screen.
class RoleProvider extends BaseProvider<LookupItem> {
  RoleProvider() : super('Roles');

  @override
  LookupItem fromJson(data) => LookupItem.fromJson(data);
}
