import 'dart:convert';

import 'package:ecommerce_mobile/core/constants/api_config.dart';
import 'package:ecommerce_mobile/providers/user_provider.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';

class AuthProvider extends ChangeNotifier {
  static AuthProvider? _active;

  bool _isAuthenticated = false;
  static String? _accesstoken;
  String? _refreshtoken;
  static Map<String, dynamic>? _accessTokenDecoded;
  String? _firstName;
  String? _lastName;
  String? _email;
  String? _role;
  int? _userId;
  String? _profileImageBase64;

  static String? get accesstoken => _accesstoken;
  String? get refreshtoken => _refreshtoken;
  static Map<String, dynamic>? get accessTokenDecoded => _accessTokenDecoded;
  String? get firstName => _firstName;
  String? get lastName => _lastName;
  String? get email => _email;
  String? get role => _role;
  int? get userId => _userId;
  String? get profileImageBase64 => _profileImageBase64;

  String get displayName {
    final name = '${_firstName ?? ''} ${_lastName ?? ''}'.trim();
    return name.isEmpty ? 'User' : name;
  }

  String _baseUrl = '';

  AuthProvider() {
    _active = this;
    _baseUrl = resolveAuthBaseUrl();
  }

  bool get isAuthenticated => _isAuthenticated;

  Future<void> login(String username, String password) async {
    final uri = Uri.parse('$_baseUrl/login');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({'username': username, 'password': password}),
    );

    if (!isValidResponse(response)) {
      throw Exception('Invalid username or password');
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    _accesstoken = data['accesstoken'] as String?;
    _refreshtoken = data['refreshtoken'] as String?;
    _isAuthenticated = true;
    _accessTokenDecoded = JwtDecoder.decode(_accesstoken ?? '');
    _applyTokenClaims(_accessTokenDecoded);
    notifyListeners();
  }

  Future<void> syncProfileFromApi(UserProvider userProvider) async {
    if (!_isAuthenticated) return;
    try {
      final user = await userProvider.getMe();
      updateFromProfile(
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email,
        profileImageBase64: user.profileImageBase64,
      );
    } catch (_) {}
  }

  void updateFromProfile({
    String? firstName,
    String? lastName,
    String? email,
    String? profileImageBase64,
  }) {
    if (firstName != null) _firstName = firstName;
    if (lastName != null) _lastName = lastName;
    if (email != null) _email = email;
    if (profileImageBase64 != null) {
      _evictProfileImageCache();
      _profileImageBase64 = profileImageBase64;
    }
    notifyListeners();
  }

  void _evictProfileImageCache() {
    final current = _profileImageBase64;
    if (current == null || current.isEmpty) return;
    try {
      final cleaned = current.contains(',') ? current.split(',').last : current;
      PaintingBinding.instance.imageCache
          .evict(MemoryImage(base64Decode(cleaned)));
    } catch (_) {}
  }

  void _applyTokenClaims(Map<String, dynamic>? claims) {
    _firstName = null;
    _lastName = null;
    _role = null;
    _userId = null;
    _email = null;
    if (claims == null) return;

    _firstName = claims['FirstName']?.toString() ?? claims['firstName']?.toString();
    _lastName = claims['LastName']?.toString() ?? claims['lastName']?.toString();
    _role = claims['Role']?.toString() ?? claims['role']?.toString();
    _email = claims['Email']?.toString() ?? claims['email']?.toString();
    final idRaw = claims['Id'] ?? claims['id'] ?? claims['nameid'];
    if (idRaw != null) {
      _userId = int.tryParse(idRaw.toString());
    }
  }

  Future<void> register({
    required String firstName,
    required String lastName,
    required String email,
    required String username,
    required String password,
  }) async {
    final uri = Uri.parse('$_baseUrl/Register');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'username': username,
        'password': password,
      }),
    );

    if (response.statusCode >= 299) {
      final message = _messageFromBody(response.body);
      throw Exception(message ?? 'Registration could not be completed');
    }
  }

  Future<String> forgotPassword(String emailOrUsername) async {
    final uri = Uri.parse('$_baseUrl/ForgotPassword');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({'emailOrUsername': emailOrUsername.trim()}),
    );
    if (response.statusCode >= 299) {
      throw Exception(
        _messageFromBody(response.body) ?? 'Could not send reset code',
      );
    }
    try {
      final data = jsonDecode(response.body);
      if (data is Map && data['message'] != null) {
        return data['message'].toString();
      }
    } catch (_) {}
    return 'If an account exists, a reset code has been sent.';
  }

  Future<String> resetPassword({
    required String emailOrUsername,
    required String code,
    required String newPassword,
    required String confirmPassword,
  }) async {
    final uri = Uri.parse('$_baseUrl/ResetPassword');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'emailOrUsername': emailOrUsername.trim(),
        'code': code.trim(),
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      }),
    );
    if (response.statusCode >= 299) {
      throw Exception(
        _messageFromBody(response.body) ?? 'Could not reset password',
      );
    }
    try {
      final data = jsonDecode(response.body);
      if (data is Map && data['message'] != null) {
        return data['message'].toString();
      }
    } catch (_) {}
    return 'Password has been reset.';
  }

  String? _messageFromBody(String body) {
    try {
      final parsed = jsonDecode(body);
      if (parsed is Map && parsed['message'] != null) {
        return parsed['message'].toString();
      }
      if (parsed is Map && parsed['title'] != null) {
        return parsed['title'].toString();
      }
    } catch (_) {}
    if (body.isNotEmpty && body.length < 200) return body;
    return null;
  }

  bool isValidResponse(http.Response response) {
    if (response.statusCode < 299) return true;
    if (response.statusCode == 401) {
      throw Exception('Invalid username or password');
    }
    throw Exception(_messageFromBody(response.body) ?? 'Something went wrong. Please try again.');
  }

  /// An access token verifies on its own, so only the API can retire it early. Signing out
  /// therefore has to reach the server; the local session is dropped either way so a failed
  /// or slow round trip never traps the user inside the app.
  Future<void> logout() async {
    final token = _accesstoken;

    if (token != null && token.isNotEmpty) {
      try {
        await http.post(
          Uri.parse('$_baseUrl/Logout'),
          headers: {...createHeaders(), 'Authorization': 'Bearer $token'},
        ).timeout(const Duration(seconds: 5));
      } catch (_) {}
    }

    _clearSession();
  }

  /// Clears tokens when any API call returns 401 (session expired / invalid JWT).
  /// The token is already worthless here, so there is nothing to tell the server.
  static void clearSessionOnUnauthorized() {
    final active = _active;
    if (active == null) {
      _accesstoken = null;
      _accessTokenDecoded = null;
      return;
    }
    active._clearSession();
  }

  void _clearSession() {
    _isAuthenticated = false;
    _accesstoken = null;
    _refreshtoken = null;
    _accessTokenDecoded = null;
    _firstName = null;
    _lastName = null;
    _email = null;
    _role = null;
    _userId = null;
    _profileImageBase64 = null;
    notifyListeners();
  }

  Map<String, String> createHeaders() => {'Content-Type': 'application/json'};
}
