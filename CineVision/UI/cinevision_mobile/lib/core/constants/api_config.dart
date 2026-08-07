import 'package:flutter/foundation.dart';

/// Resolves the Web API base URL for the current platform.
///
/// Override at build/run time (RS2 upute):
/// `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5126/`
String resolveApiBaseUrl() {
  const fromEnv = String.fromEnvironment('API_BASE_URL');
  var url = fromEnv.isNotEmpty
      ? fromEnv
      : (defaultTargetPlatform == TargetPlatform.android
          ? 'http://10.0.2.2:5126/'
          : 'http://localhost:5126/');

  if (!url.endsWith('/')) {
    url = '$url/';
  }
  return url;
}

/// Auth controller base: `{API_BASE_URL}Access`.
String resolveAuthBaseUrl() => '${resolveApiBaseUrl()}Access';
