import 'dart:convert';



import 'package:ecommerce_desktop/models/analytics.dart';

import 'package:ecommerce_desktop/providers/auth_provider.dart';

import 'package:ecommerce_desktop/providers/base_provider.dart';

import 'package:flutter/material.dart';

import 'package:http/http.dart' as http;

import 'package:signalr_netcore/signalr_client.dart';



class AnalyticsProvider with ChangeNotifier {

  HubConnection? _hubConnection;

  bool _isConnecting = false;

  bool _isLiveConnected = false;

  AnalyticsLiveSnapshot? _liveSnapshot;



  bool get isLiveConnected => _isLiveConnected;

  AnalyticsLiveSnapshot? get liveSnapshot => _liveSnapshot;

  DashboardStats? get liveDashboard => _liveSnapshot?.dashboard;



  Future<DashboardStats> getDashboard() async {

    return _get('Analytics/Dashboard', DashboardStats.fromJson);

  }



  Future<List<MoviePerformance>> getMoviePerformance() async {

    final data = await _getList('Analytics/MoviePerformance');

    return data.map((e) => MoviePerformance.fromJson(e)).toList();

  }



  Future<List<TimeSlotPerformance>> getTimeSlotPerformance() async {

    final data = await _getList('Analytics/PerformanceByTimeSlot');

    return data.map((e) => TimeSlotPerformance.fromJson(e)).toList();

  }



  Future<List<HallUtilization>> getHallUtilization() async {

    final data = await _getList('Analytics/HallUtilization');

    return data.map((e) => HallUtilization.fromJson(e)).toList();

  }



  Future<void> connectRealtime() async {

    if (_isConnecting || _isLiveConnected) return;

  if (AuthProvider.accesstoken == null || AuthProvider.accesstoken!.isEmpty) {

      return;

    }



    _isConnecting = true;

    notifyListeners();



    try {

      final base = BaseProvider.baseUrl ?? 'http://localhost:5126/';

      final hubUrl = '${Uri.parse(base).origin}/hubs/analytics';



      _hubConnection = HubConnectionBuilder()

          .withUrl(

            hubUrl,

            options: HttpConnectionOptions(

              accessTokenFactory: () async => AuthProvider.accesstoken ?? '',

            ),

          )

          .withAutomaticReconnect()

          .build();



      _hubConnection!.on('AnalyticsUpdated', _handleAnalyticsUpdated);

      _hubConnection!.onclose(({error}) {

        _isLiveConnected = false;

        notifyListeners();

      });

      _hubConnection!.onreconnected(({connectionId}) {

        _isLiveConnected = true;

        notifyListeners();

      });



      await _hubConnection!.start();

      _isLiveConnected = true;

    } catch (_) {

      _isLiveConnected = false;

    } finally {

      _isConnecting = false;

      notifyListeners();

    }

  }



  Future<void> disconnectRealtime() async {

    try {

      await _hubConnection?.stop();

    } catch (_) {}

    _hubConnection = null;

    _isLiveConnected = false;

    _isConnecting = false;

    _liveSnapshot = null;

    notifyListeners();

  }



  void _handleAnalyticsUpdated(List<Object?>? arguments) {

    if (arguments == null || arguments.isEmpty) return;



    final payload = arguments.first;

    if (payload is! Map) return;



  final snapshot = AnalyticsLiveSnapshot.fromJson(

      Map<String, dynamic>.from(payload),

    );

    _liveSnapshot = snapshot;

    notifyListeners();

  }



  Future<T> _get<T>(

    String path,

    T Function(Map<String, dynamic>) fromJson,

  ) async {

    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';

    final uri = Uri.parse('$baseUrl$path');

    final response = await http.get(uri, headers: _headers());

    if (response.statusCode >= 299) {

      throw Exception('Failed to load analytics');

    }

    return fromJson(jsonDecode(response.body) as Map<String, dynamic>);

  }



  Future<List<Map<String, dynamic>>> _getList(String path) async {

    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';

    final uri = Uri.parse('$baseUrl$path');

    final response = await http.get(uri, headers: _headers());

    if (response.statusCode >= 299) {

      throw Exception('Failed to load analytics');

    }

    final data = jsonDecode(response.body);

    if (data is List) {

      return data.cast<Map<String, dynamic>>();

    }

    return [];

  }



  Map<String, String> _headers() => {

        'Content-Type': 'application/json',

        'Authorization': 'Bearer ${AuthProvider.accesstoken ?? ''}',

      };

}


