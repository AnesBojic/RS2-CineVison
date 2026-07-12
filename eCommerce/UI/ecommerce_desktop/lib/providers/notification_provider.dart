import 'dart:convert';

import 'package:ecommerce_desktop/models/notification.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class NotificationProvider with ChangeNotifier {
  int _unreadCount = 0;
  List<AppNotification> _items = [];
  bool _loading = false;

  int get unreadCount => _unreadCount;
  List<AppNotification> get items => _items;
  bool get loading => _loading;

  String get _baseUrl => BaseProvider.baseUrl ?? 'http://localhost:5126/';

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthProvider.accesstoken ?? ''}',
      };

  Future<void> refresh() async {
    _loading = true;
    notifyListeners();

    try {
      final countResponse = await http.get(
        Uri.parse('${_baseUrl}Notifications/UnreadCount'),
        headers: _headers,
      );
      _validate(countResponse);
      _unreadCount = jsonDecode(countResponse.body) as int? ?? 0;

      final listResponse = await http.get(
        Uri.parse('${_baseUrl}Notifications?limit=30'),
        headers: _headers,
      );
      _validate(listResponse);
      final list = jsonDecode(listResponse.body) as List<dynamic>;
      _items = list
          .map((e) => AppNotification.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList();
    } catch (_) {
      // Keep previous values on transient failures.
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> markAsRead(int id) async {
    final response = await http.put(
      Uri.parse('${_baseUrl}Notifications/$id/Read'),
      headers: _headers,
    );
    _validate(response);
    await refresh();
  }

  Future<void> markAllRead({String? type}) async {
    final uri = type == null
        ? Uri.parse('${_baseUrl}Notifications/ReadAll')
        : Uri.parse('${_baseUrl}Notifications/ReadAll?type=${Uri.encodeComponent(type)}');
    final response = await http.put(uri, headers: _headers);
    _validate(response);
    await refresh();
  }

  void _validate(http.Response response) {
    if (response.statusCode >= 299) {
      throw Exception('Notification request failed (${response.statusCode})');
    }
  }
}
