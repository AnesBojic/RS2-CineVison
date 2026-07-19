import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class AuthProvider extends ChangeNotifier {
  bool _isAuthenticated = false;
  static String? _accesstoken;
  String? _refreshtoken;
  String? _firstName;
  String? _lastName;
  String? _role;
  int? _userId;
  String? _email;
  String? _profileImageBase64;

  static String? get accesstoken => _accesstoken;
  String? get refreshtoken => _refreshtoken;
  String? get firstName => _firstName;
  String? get lastName => _lastName;
  String? get role => _role;
  int? get userId => _userId;
  String? get email => _email;
  String? get profileImageBase64 => _profileImageBase64;

  String get displayName {
    final name = '${_firstName ?? ''} ${_lastName ?? ''}'.trim();
    return name.isEmpty ? 'User' : name;
  }

  bool get isAdmin => (_role ?? '').toLowerCase() == 'admin';
  bool get isStaff => (_role ?? '').toLowerCase() == 'staff';

  String _baseUrl = '';

  AuthProvider() {
    _baseUrl = const String.fromEnvironment(
      'BASE_URL',
      defaultValue: 'http://localhost:5126/Access',
    );
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
    _applyTokenClaims(_accesstoken);

    if (!isAdmin && !isStaff) {
      _clearSession();
      throw Exception(
        'You do not have authorization to access the desktop application.',
      );
    }

    _isAuthenticated = true;
    notifyListeners();
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
      PaintingBinding.instance.imageCache.evict(MemoryImage(base64Decode(current)));
    } catch (_) {}
  }

  void _applyTokenClaims(String? token) {
    _firstName = null;
    _lastName = null;
    _role = null;
    _userId = null;
    _email = null;
    if (token == null || token.isEmpty) return;

    try {
      final parts = token.split('.');
      if (parts.length != 3) return;
      final normalized = base64Url.normalize(parts[1]);
      final payload = jsonDecode(utf8.decode(base64Url.decode(normalized)))
          as Map<String, dynamic>;
      _firstName = payload['FirstName']?.toString() ?? payload['firstName']?.toString();
      _lastName = payload['LastName']?.toString() ?? payload['lastName']?.toString();
      _role = payload['Role']?.toString() ?? payload['role']?.toString();
      _email = payload['Email']?.toString() ?? payload['email']?.toString();
      final idRaw = payload['Id'] ?? payload['id'] ?? payload['nameid'];
      if (idRaw != null) {
        _userId = int.tryParse(idRaw.toString());
      }
    } catch (_) {}
  }

  bool isValidResponse(http.Response response) {
    if (response.statusCode < 299) return true;
    if (response.statusCode == 401) {
      throw Exception('Unauthorized');
    }
    throw Exception('Something went wrong. Please try again.');
  }

  void logout() {
    _clearSession();
    notifyListeners();
  }

  void _clearSession() {
    _isAuthenticated = false;
    _accesstoken = null;
    _refreshtoken = null;
    _firstName = null;
    _lastName = null;
    _role = null;
    _userId = null;
    _email = null;
    _profileImageBase64 = null;
  }

  Map<String, String> createHeaders() => {'Content-Type': 'application/json'};
}
