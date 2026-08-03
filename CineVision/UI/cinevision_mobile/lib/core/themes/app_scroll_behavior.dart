import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';

/// Shows scrollbars on all scrollables (movies, booking, checkout, profile, etc.).
class AppScrollBehavior extends MaterialScrollBehavior {
  const AppScrollBehavior();

  @override
  Set<PointerDeviceKind> get dragDevices => {
        PointerDeviceKind.touch,
        PointerDeviceKind.mouse,
        PointerDeviceKind.stylus,
        PointerDeviceKind.trackpad,
      };

  @override
  Widget buildScrollbar(
    BuildContext context,
    Widget child,
    ScrollableDetails details,
  ) {
    // Only show a scrollbar on vertical lists so horizontal rows
    // (seat map, date chips) do not get overlapping thumbs.
    final isVertical = details.direction == AxisDirection.down ||
        details.direction == AxisDirection.up;
    if (!isVertical) return child;

    return Scrollbar(
      controller: details.controller,
      thumbVisibility: true,
      trackVisibility: true,
      thickness: 6,
      radius: const Radius.circular(4),
      child: child,
    );
  }
}
