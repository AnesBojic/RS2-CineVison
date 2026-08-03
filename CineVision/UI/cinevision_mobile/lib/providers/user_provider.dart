
import 'dart:convert';

import 'package:cinevision_mobile/providers/base_provider.dart';
import 'package:http/http.dart' as http;

import '../models/user.dart';

class UserProvider extends BaseProvider<User> {
  UserProvider() : super("Users");

  @override
  User fromJson(data) {
    return User.fromJson(data);
  }

  Future<User> getMe() async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/Me');
    final response = await http.get(uri, headers: createHeaders());
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<User> updateMe(Map<String, dynamic> request) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/Me');
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonEncode(request),
    );
    validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<void> changePassword({
    required int userId,
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
  }) async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/ChangePassword');
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

  Future<dynamic> changePasswordLegacy(dynamic object) async {
    var url = "${BaseProvider.baseUrl}$endpoint/ChangePassword";

    var uri = Uri.parse(url);

    var jsonRequest = jsonEncode(object);
    var headers = createHeaders();

    http.Response response = await http.put(uri, headers: headers, body: jsonRequest);

    validateResponse(response);
  }
}
