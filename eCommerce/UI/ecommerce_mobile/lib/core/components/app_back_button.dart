import 'package:flutter/material.dart';

class AppBackButton extends StatelessWidget {
  /// Custom back button used by the pages that are pushed on top of the entry point.
  const AppBackButton({
    super.key,
  });

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: const Icon(Icons.arrow_back),
      onPressed: () => Navigator.pop(context),
    );
  }
}
