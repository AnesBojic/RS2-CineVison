import 'dart:convert';

import 'package:ecommerce_desktop/models/chat_message.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class ChatBotProvider with ChangeNotifier {
  Future<ChatResponse> sendMessage(
    String message,
    List<ChatMessage> history,
  ) async {
    final baseUrl = BaseProvider.baseUrl ?? 'http://localhost:5126/';
    final uri = Uri.parse('${baseUrl}ChatBot/Chat');
    final body = jsonEncode({
      'message': message,
      'history': history.map((e) => e.toJson()).toList(),
    });
    final response = await http.post(
      uri,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthProvider.accesstoken ?? ''}',
      },
      body: body,
    );
    if (response.statusCode >= 299) {
      if (response.statusCode == 401) {
        AuthProvider.clearSessionOnUnauthorized();
      }
      final parsed = ApiErrorParser.messageFromBody(response.body);
      throw ApiClientException(
        parsed ??
            (response.statusCode == 401
                ? 'Your session has expired. Please sign in again.'
                : 'Chat request failed (${response.statusCode}).'),
      );
    }
    return ChatResponse.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }
}
