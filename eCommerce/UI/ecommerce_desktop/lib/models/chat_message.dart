import '../core/utils/utc_datetime.dart';

class ChatMessage {
  final String role;
  final String content;

  ChatMessage({required this.role, required this.content});

  Map<String, dynamic> toJson() => {'role': role, 'content': content};
}

class ChatResponse {
  final String reply;
  final DateTime? repliedAt;

  ChatResponse({required this.reply, this.repliedAt});

  factory ChatResponse.fromJson(Map<String, dynamic> json) {
    return ChatResponse(
      reply: json['reply'] as String? ?? '',
      repliedAt: UtcDateTime.tryParse(json['repliedAt']),
    );
  }
}
