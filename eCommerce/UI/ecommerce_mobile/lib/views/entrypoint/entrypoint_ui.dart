import 'package:flutter/material.dart';

import '../movies/movies_page.dart';

/// Main customer shell — browse movies (mockup step 1).
class EntryPointUI extends StatelessWidget {
  const EntryPointUI({super.key});

  @override
  Widget build(BuildContext context) {
    return const MoviesPage();
  }
}
