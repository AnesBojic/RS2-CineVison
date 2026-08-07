/// Resolves the Web API base URL.
///
/// Override at build/run time (RS2 upute):
/// `flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5126/`
String resolveApiBaseUrl() {
  const fromEnv = String.fromEnvironment('API_BASE_URL');
  var url = fromEnv.isNotEmpty ? fromEnv : 'http://localhost:5126/';

  if (!url.endsWith('/')) {
    url = '$url/';
  }
  return url;
}

/// Auth controller base: `{API_BASE_URL}Access`.
String resolveAuthBaseUrl() => '${resolveApiBaseUrl()}Access';
