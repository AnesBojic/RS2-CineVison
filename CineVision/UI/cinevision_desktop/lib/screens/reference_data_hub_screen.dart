import 'package:cinevision_desktop/core/enums/role_permissions.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/providers/age_rating_provider.dart';
import 'package:cinevision_desktop/providers/auth_provider.dart';
import 'package:cinevision_desktop/providers/hall_status_provider.dart';
import 'package:cinevision_desktop/providers/language_provider.dart';
import 'package:cinevision_desktop/providers/role_provider.dart';
import 'package:cinevision_desktop/providers/screen_type_provider.dart';
import 'package:cinevision_desktop/screens/genre_list_screen.dart';
import 'package:cinevision_desktop/screens/lookup_list_screen.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Single place to manage every reference (lookup) table the cinema uses.
class ReferenceDataHubScreen extends StatefulWidget {
  const ReferenceDataHubScreen({super.key});

  @override
  State<ReferenceDataHubScreen> createState() => _ReferenceDataHubScreenState();
}

class _ReferenceDataHubScreenState extends State<ReferenceDataHubScreen> {
  int _section = 0;

  List<int> _visibleSectionIds(AuthProvider auth) {
    final ids = <int>[];
    if (auth.hasPermission(RolePermissions.manageReferenceData)) {
      ids.addAll(const [0, 1, 2, 3, 4]);
    }
    if (auth.hasPermission(RolePermissions.manageRoles)) {
      ids.add(5);
    }
    return ids;
  }

  Widget _sectionBody() {
    switch (_section) {
      case 1:
        return const LookupListScreen<ScreenTypeProvider>(
          key: ValueKey('screen-types-section'),
          title: 'Screen Types',
          itemNoun: 'screen type',
        );
      case 2:
        return const LookupListScreen<HallStatusProvider>(
          key: ValueKey('hall-statuses-section'),
          title: 'Hall Statuses',
          itemNoun: 'hall status',
          extraField: LookupExtraField.allowsProjections,
        );
      case 3:
        return const LookupListScreen<AgeRatingProvider>(
          key: ValueKey('age-ratings-section'),
          title: 'Age Ratings',
          itemNoun: 'age rating',
          extraField: LookupExtraField.minimumAge,
        );
      case 4:
        return const LookupListScreen<LanguageProvider>(
          key: ValueKey('languages-section'),
          title: 'Languages',
          itemNoun: 'language',
          extraField: LookupExtraField.code,
        );
      case 5:
        return const LookupListScreen<RoleProvider>(
          key: ValueKey('roles-section'),
          title: 'Roles',
          itemNoun: 'role',
          lockAuthorizationRoleNames: true,
          extraField: LookupExtraField.roleAccess,
        );
      default:
        return const GenreListScreen(key: ValueKey('genres-section'));
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final visibleIds = _visibleSectionIds(auth);
    if (visibleIds.isNotEmpty && !visibleIds.contains(_section)) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) setState(() => _section = visibleIds.first);
      });
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(32, 16, 32, 0),
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final i in visibleIds)
                SectionChip(
                  label: _sectionLabel(i),
                  selected: _section == i,
                  onTap: () => setState(() => _section = i),
                ),
            ],
          ),
        ),
        Expanded(child: _sectionBody()),
      ],
    );
  }

  static String _sectionLabel(int i) => const [
        'Genres',
        'Screen Types',
        'Hall Statuses',
        'Age Ratings',
        'Languages',
        'Roles',
      ][i];
}
