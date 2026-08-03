import 'dart:convert';
import 'dart:typed_data';

import 'package:image/image.dart' as img;

/// Compresses and resizes poster images before upload to avoid huge API payloads.
Future<String> preparePosterBase64(Uint8List bytes, {int maxWidth = 600}) async {
  final decoded = img.decodeImage(bytes);
  if (decoded == null) {
    return base64Encode(bytes);
  }

  final resized = decoded.width > maxWidth
      ? img.copyResize(decoded, width: maxWidth)
      : decoded;

  return base64Encode(img.encodeJpg(resized, quality: 82));
}
