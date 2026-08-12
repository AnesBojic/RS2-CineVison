import 'dart:convert';

import 'package:cinevision_desktop/models/user.dart';
import 'package:cinevision_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class UserProvider extends BaseProvider<User> {
  UserProvider() : super("Users");

  @override
  User fromJson(data) {
    return User.fromJson(data);
  }

  Future<User> getMe() async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/Me');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<User> updateMe(Map<String, dynamic> request) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/Me');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode(request),
    );
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
    required int userId,
  }) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/ChangePassword');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode({
        'id': userId,
        'password': currentPassword,
        'newPassword': newPassword,
        'confirmNewPassword': confirmPassword,
      }),
    );
    validateResponse(response);
  }

  Future<void> sendEmail(int userId, String subject, String body) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/$userId/SendEmail');
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonEncode({'subject': subject, 'body': body, 'isHtml': false}),
    );
    validateResponse(response);
  }

  Future<void> setActive(int userId, bool isActive) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/$userId/Active');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode({'isActive': isActive}),
    );
    validateResponse(response);
  }

  Future<Map<String, dynamic>> getDeleteImpact(int userId) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}Users/$userId/DeleteImpact');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    return jsonDecode(response.body) as Map<String, dynamic>;
  }
}
