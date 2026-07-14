import 'package:flutter/foundation.dart';

/// Resolves the Web API base URL for the current platform.
///
/// Override at build/run time with:
/// `flutter run --dart-define=baseUrl=http://192.168.x.x:5126/`
String resolveApiBaseUrl() {
  const fromEnv = String.fromEnvironment('baseUrl');
  if (fromEnv.isNotEmpty) return fromEnv;

  if (defaultTargetPlatform == TargetPlatform.android) {
    return 'http://10.0.2.2:5126/';
  }

  return 'http://localhost:5126/';
}

String resolveAuthBaseUrl() {
  const fromEnv = String.fromEnvironment('BASE_URL');
  if (fromEnv.isNotEmpty) return fromEnv;

  return '${resolveApiBaseUrl()}Access';
}
