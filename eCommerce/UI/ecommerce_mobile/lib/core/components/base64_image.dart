import 'dart:typed_data';

import 'package:ecommerce_mobile/utils/base64_image_cache.dart';
import 'package:flutter/material.dart';

import '../constants/app_defaults.dart';

class Base64ImageWithLoader extends StatelessWidget {
  final BoxFit fit;

  /// This widget is used for displaying base64 image with the same style
  /// as NetworkImageWithLoader.
  const Base64ImageWithLoader(
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
    final imageBytes = Base64ImageCache.decode(src);

    return ClipRRect(
      borderRadius: borderRadius ?? BorderRadius.all(Radius.circular(radius)),
      child: imageBytes == null
          ? const Icon(Icons.error)
          : Container(
              decoration: BoxDecoration(
                image: DecorationImage(
                  image: MemoryImage(imageBytes),
                  fit: fit,
                ),
              ),
            ),
    );
  }
}
