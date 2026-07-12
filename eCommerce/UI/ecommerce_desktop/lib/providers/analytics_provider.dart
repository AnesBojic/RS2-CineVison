import 'dart:convert';

import 'package:ecommerce_desktop/models/analytics.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class AnalyticsProvider with ChangeNotifier {
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
