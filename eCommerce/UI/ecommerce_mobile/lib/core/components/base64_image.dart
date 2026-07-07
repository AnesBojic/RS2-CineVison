import 'dart:convert';
import 'dart:typed_data';

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

  Uint8List? _decodeBase64Image(String base64Image) {
    try {
      // Supports both plain base64 and data URLs:
      // data:image/png;base64,....
      final cleanedBase64 = base64Image.contains(',')
          ? base64Image.split(',').last
          : base64Image;

      return base64Decode(cleanedBase64);
    } catch (_) {
      return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final imageBytes = _decodeBase64Image(src);

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