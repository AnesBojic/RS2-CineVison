import 'dart:convert';

import 'package:ecommerce_mobile/models/app_notification.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:signalr_netcore/signalr_client.dart';

class NotificationProvider with ChangeNotifier {
  int _unreadCount = 0;
  List<AppNotification> _items = [];
  bool _loading = false;
  HubConnection? _hubConnection;
  bool _connecting = false;
  bool _liveConnected = false;

  int get unreadCount => _unreadCount;
  List<AppNotification> get items => _items;
  bool get loading => _loading;

  String get _baseUrl => BaseProvider.baseUrl ?? 'http://localhost:5126/';

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthProvider.accesstoken ?? ''}',
      };

  Future<void> connectRealtime() async {
    if (_connecting || _liveConnected) return;
    if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {
      return;
    }

    _connecting = true;
    try {
      final hubUrl = '${Uri.parse(_baseUrl).origin}/hubs/notifications';
      _hubConnection = HubConnectionBuilder()
          .withUrl(
            hubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () async => AuthProvider.accesstoken ?? '',
            ),
          )
          .withAutomaticReconnect()
          .build();

      _hubConnection!.on('NotificationReceived', _onNotificationReceived);
      _hubConnection!.on('UnreadCountUpdated', _onUnreadCountUpdated);
      _hubConnection!.onclose(({error}) {
        _liveConnected = false;
        notifyListeners();
      });
      _hubConnection!.onreconnected(({connectionId}) {
        _liveConnected = true;
        refresh();
        notifyListeners();
      });

      await _hubConnection!.start();
      _liveConnected = true;
    } catch (_) {
      _liveConnected = false;
    } finally {
      _connecting = false;
      notifyListeners();
    }
  }

  Future<void> disconnectRealtime() async {
    final hub = _hubConnection;
    _hubConnection = null;
    _liveConnected = false;
    if (hub != null) {
      try {
        await hub.stop();
      } catch (_) {}
    }
  }

  void _onNotificationReceived(List<Object?>? args) {
    if (args == null || args.isEmpty) return;
    final payload = args.first;
    if (payload is! Map) return;

    final map = Map<String, dynamic>.from(payload);
    final unread = map['unreadCount'];
    if (unread is int) {
      _unreadCount = unread;
    } else if (unread is num) {
      _unreadCount = unread.toInt();
    }

    final n = map['notification'];
    if (n is Map) {
      final item = AppNotification.fromJson(Map<String, dynamic>.from(n));
      if (item.id != null && item.id! > 0) {
        _items = [item, ..._items.where((e) => e.id != item.id)];
      }
    }
    notifyListeners();
  }

  void _onUnreadCountUpdated(List<Object?>? args) {
    if (args == null || args.isEmpty) return;
    final value = args.first;
    if (value is int) {
      _unreadCount = value;
    } else if (value is num) {
      _unreadCount = value.toInt();
    }
    notifyListeners();
  }

  Future<void> refresh() async {
    if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {
      _items = [];
      _unreadCount = 0;
      notifyListeners();
      return;
    }

    _loading = true;
    notifyListeners();
    try {
      // Unread count and the inbox list are independent GETs.
      final responses = await Future.wait([
        http.get(
          Uri.parse('${_baseUrl}Notifications/UnreadCount'),
          headers: _headers,
        ),
        http.get(
          Uri.parse('${_baseUrl}Notifications?limit=50'),
          headers: _headers,
        ),
      ]);

      final countResponse = responses[0];
      if (countResponse.statusCode < 299) {
        _unreadCount = jsonDecode(countResponse.body) as int? ?? 0;
      }

      final listResponse = responses[1];
      if (listResponse.statusCode < 299) {
        final list = jsonDecode(listResponse.body) as List<dynamic>;
        _items = list
            .map((e) => AppNotification.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      }
    } catch (_) {
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
    if (response.statusCode >= 299) {
      throw Exception('Could not mark notification as read.');
    }
    await refresh();
  }

  Future<void> markAllRead() async {
    final response = await http.put(
      Uri.parse('${_baseUrl}Notifications/ReadAll'),
      headers: _headers,
    );
    if (response.statusCode >= 299) {
      throw Exception('Could not mark notifications as read.');
    }
    await refresh();
  }
}
