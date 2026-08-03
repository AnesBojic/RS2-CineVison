import 'dart:collection';
import 'dart:convert';
import 'dart:typed_data';

/// Small LRU cache so base64 posters are decoded once per unique payload.
class Base64ImageCache {
  Base64ImageCache._();

  static const int _maxEntries = 64;
  static final LinkedHashMap<String, Uint8List> _cache =
      LinkedHashMap<String, Uint8List>();

  static Uint8List? decode(String? base64Image) {
    if (base64Image == null || base64Image.isEmpty) {
      return null;
    }

    final cached = _cache[base64Image];
    if (cached != null) {
      // Refresh LRU order.
      _cache.remove(base64Image);
      _cache[base64Image] = cached;
      return cached;
    }

    try {
      final cleaned = base64Image.contains(',')
          ? base64Image.split(',').last
          : base64Image;
      final bytes = base64Decode(cleaned);
      _cache[base64Image] = bytes;
      while (_cache.length > _maxEntries) {
        _cache.remove(_cache.keys.first);
      }
      return bytes;
    } catch (_) {
      return null;
    }
  }
}
