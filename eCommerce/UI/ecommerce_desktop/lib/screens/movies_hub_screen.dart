import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/screens/genre_list_screen.dart';
import 'package:ecommerce_desktop/screens/movie_list_screen.dart';
import 'package:flutter/material.dart';

/// Movies area with Genres as an inner section (not a separate sidebar item).
class MoviesHubScreen extends StatefulWidget {
  const MoviesHubScreen({super.key, this.editId, this.onEditConsumed});

  final int? editId;
  final VoidCallback? onEditConsumed;

  @override
  State<MoviesHubScreen> createState() => _MoviesHubScreenState();
}

class _MoviesHubScreenState extends State<MoviesHubScreen> {
  int _section = 0; // 0 = movies, 1 = genres

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(32, 16, 32, 0),
          child: Row(
            children: [
              _SectionChip(
                label: 'Movies',
                selected: _section == 0,
                onTap: () => setState(() => _section = 0),
              ),
              const SizedBox(width: 8),
              _SectionChip(
                label: 'Genres',
                selected: _section == 1,
                onTap: () => setState(() => _section = 1),
              ),
            ],
          ),
        ),
        Expanded(
          child: _section == 0
              ? MovieListScreen(
                  key: const ValueKey('movies-section'),
                  editId: widget.editId,
                  onEditConsumed: widget.onEditConsumed,
                )
              : const GenreListScreen(
                  key: ValueKey('genres-section'),
                ),
        ),
      ],
    );
  }
}

class _SectionChip extends StatelessWidget {
  const _SectionChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? AppColors.primary : AppColors.inputFill,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Text(
            label,
            style: TextStyle(
              color: selected ? Colors.white : AppColors.textSecondary,
              fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
              fontSize: 13,
            ),
          ),
        ),
      ),
    );
  }
}
