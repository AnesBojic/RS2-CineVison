import 'package:flutter/material.dart';

import '../constants/app_defaults.dart';

class AssetImageWithLoader extends StatelessWidget {
  final BoxFit fit;

  /// This widget is used for displaying asset images with the same style
  /// as NetworkImageWithLoader.
  const AssetImageWithLoader(
    this.src, {
    super.key,
    this.fit = BoxFit.cover,
    this.radius = AppDefaults.radius,
    this.borderRadius,
  });

  final String src;
  final double radius;
  final BorderRadius? borderRadius;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: borderRadius ?? BorderRadius.all(Radius.circular(radius)),
      child: Container(
        decoration: BoxDecoration(
          image: DecorationImage(
            image: AssetImage(src),
            fit: fit,
          ),
        ),
      ),
    );
  }
}