import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/providers/age_rating_provider.dart';
import 'package:cinevision_desktop/providers/hall_status_provider.dart';
import 'package:cinevision_desktop/providers/language_provider.dart';
import 'package:cinevision_desktop/providers/screen_type_provider.dart';
import 'package:cinevision_desktop/screens/genre_list_screen.dart';
import 'package:cinevision_desktop/screens/lookup_list_screen.dart';
import 'package:flutter/material.dart';

/// Single place to manage every reference (lookup) table the cinema uses.
class ReferenceDataHubScreen extends StatefulWidget {
  const ReferenceDataHubScreen({super.key});

  @override
  State<ReferenceDataHubScreen> createState() => _ReferenceDataHubScreenState();
}

class _ReferenceDataHubScreenState extends State<ReferenceDataHubScreen> {
  int _section = 0;

  static const _sections = [
    'Genres',
    'Screen Types',
    'Hall Statuses',
    'Age Ratings',
    'Languages',
  ];

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
      default:
        return const GenreListScreen(key: ValueKey('genres-section'));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(32, 16, 32, 0),
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (var i = 0; i < _sections.length; i++)
                SectionChip(
                  label: _sections[i],
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
}
