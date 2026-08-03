import 'dart:convert';

import 'package:cinevision_desktop/core/enums/api_enums.dart';
import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/models/chat_message.dart';
import 'package:cinevision_desktop/providers/auth_provider.dart';
import 'package:cinevision_desktop/providers/chatbot_provider.dart';
import 'package:cinevision_desktop/providers/notification_provider.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ChatBotScreen extends StatefulWidget {
  const ChatBotScreen({super.key});

  @override
  State<ChatBotScreen> createState() => _ChatBotScreenState();
}

class _ChatBotScreenState extends State<ChatBotScreen> {
  final _controller = TextEditingController();
  final _scrollController = ScrollController();
  final List<_ChatBubble> _messages = [];
  bool _sending = false;
  bool _historyLoaded = false;

  String get _historyKey {
    final userId = context.read<AuthProvider>().userId;
    return 'chatbot_history_${userId ?? 'unknown'}';
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_historyLoaded) {
      _historyLoaded = true;
      _loadHistory();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(32, 24, 32, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Cinema Assistant Chatbot',
                  style: TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              TextButton.icon(
                onPressed: _messages.isEmpty ? null : _clearHistory,
                icon: const Icon(Icons.delete_outline, size: 18),
                label: const Text('Clear history'),
              ),
            ],
          ),
          const SizedBox(height: 6),
          const Text(
            'Ask me anything about managing your cinema.',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 14),
          ),
          const SizedBox(height: 20),
          Expanded(
            child: Container(
              decoration: AppDecorations.card(radius: 14),
              child: Column(
                children: [
                  Expanded(
                    child: Scrollbar(
                      controller: _scrollController,
                      thumbVisibility: true,
                      child: ListView.builder(
                        controller: _scrollController,
                        padding: const EdgeInsets.all(24),
                        itemCount: _messages.length,
                        itemBuilder: (context, index) => _buildBubble(_messages[index]),
                      ),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.fromLTRB(16, 12, 12, 16),
                    decoration: BoxDecoration(
                      border: Border(top: BorderSide(color: AppColors.divider)),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _controller,
                            enabled: !_sending,
                            style: const TextStyle(color: AppColors.textPrimary),
                            decoration: InputDecoration(
                              hintText: 'Type your message...',
                              filled: true,
                              fillColor: AppColors.inputFill,
                              border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(12),
                                borderSide: BorderSide.none,
                              ),
                              contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                            ),
                            onSubmitted: (_) => _send(),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Material(
                          color: AppColors.primary,
                          borderRadius: BorderRadius.circular(12),
                          child: InkWell(
                            borderRadius: BorderRadius.circular(12),
                            onTap: _sending ? null : _send,
                            child: SizedBox(
                              width: 48,
                              height: 48,
                              child: _sending
                                  ? const Padding(
                                      padding: EdgeInsets.all(14),
                                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                                    )
                                  : const Icon(Icons.send_rounded, color: Colors.white, size: 20),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBubble(_ChatBubble bubble) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 18),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: bubble.isBot ? MainAxisAlignment.start : MainAxisAlignment.end,
        children: [
          if (bubble.isBot) ...[
            CircleAvatar(
              radius: 18,
              backgroundColor: AppColors.blue.withValues(alpha: 0.15),
              child: const Icon(Icons.smart_toy_outlined, color: AppColors.blue, size: 20),
            ),
            const SizedBox(width: 12),
          ],
          Flexible(
            child: Column(
              crossAxisAlignment: bubble.isBot ? CrossAxisAlignment.start : CrossAxisAlignment.end,
              children: [
                Container(
                  constraints: const BoxConstraints(maxWidth: 560),
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  decoration: BoxDecoration(
                    color: bubble.isBot ? AppColors.inputFill : AppColors.primary.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.only(
                      topLeft: const Radius.circular(14),
                      topRight: const Radius.circular(14),
                      bottomLeft: Radius.circular(bubble.isBot ? 4 : 14),
                      bottomRight: Radius.circular(bubble.isBot ? 14 : 4),
                    ),
                    border: Border.all(
                      color: bubble.isBot ? AppColors.cardBorder : AppColors.primary.withValues(alpha: 0.25),
                    ),
                  ),
                  child: Text(
                    bubble.text,
                    style: const TextStyle(color: AppColors.textPrimary, height: 1.45),
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  DateFormat('h:mm a').format(bubble.time),
                  style: const TextStyle(color: AppColors.textSecondary, fontSize: 11),
                ),
              ],
            ),
          ),
          if (!bubble.isBot) const SizedBox(width: 4),
        ],
      ),
    );
  }

  Future<void> _send() async {
    final text = _controller.text.trim();
    if (text.isEmpty) return;
    final chatbot = context.read<ChatBotProvider>();

    setState(() {
      _messages.add(_ChatBubble(isBot: false, text: text, time: DateTime.now()));
      _controller.clear();
      _sending = true;
    });
    await _saveHistory();
    _scrollToBottom();

    try {
      final history = _messages
          .where((m) => m != _messages.last)
          .map((m) => ChatMessage(role: m.isBot ? 'assistant' : 'user', content: m.text))
          .toList();

      final response = await chatbot.sendMessage(text, history);

      if (!mounted) return;
      setState(() {
        _messages.add(_ChatBubble(
          isBot: true,
          text: response.reply,
          time: response.repliedAt?.toLocal() ?? DateTime.now(),
        ));
        _sending = false;
      });
      await _saveHistory();
      _scrollToBottom();
      if (mounted) {
        context.read<NotificationProvider>().markAllRead(
              type: NotificationTypes.message,
            );
      }
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _sending = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _loadHistory() async {
    final historyKey = _historyKey;
    final preferences = await SharedPreferences.getInstance();
    final stored = preferences.getString(historyKey);
    final restored = <_ChatBubble>[];

    if (stored != null) {
      try {
        final decoded = jsonDecode(stored);
        if (decoded is List) {
          restored.addAll(
            decoded
                .whereType<Map>()
                .map((item) => _ChatBubble.fromJson(
                      Map<String, dynamic>.from(item),
                    )),
          );
        }
      } catch (_) {
        await preferences.remove(historyKey);
      }
    }

    if (restored.isEmpty) {
      restored.add(_welcomeMessage());
    }

    if (!mounted) return;
    setState(() {
      _messages
        ..clear()
        ..addAll(restored);
    });
    _scrollToBottom();
  }

  Future<void> _saveHistory() async {
    final historyKey = _historyKey;
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(
      historyKey,
      jsonEncode(_messages.map((message) => message.toJson()).toList()),
    );
  }

  Future<void> _clearHistory() async {
    final historyKey = _historyKey;
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove(historyKey);
    if (!mounted) return;
    setState(() {
      _messages
        ..clear()
        ..add(_welcomeMessage());
    });
    _scrollToBottom();
  }

  _ChatBubble _welcomeMessage() => _ChatBubble(
        isBot: true,
        text: "Hello! I'm your cinema assistant. How can I help you today?",
        time: DateTime.now(),
      );
}

class _ChatBubble {
  _ChatBubble({required this.isBot, required this.text, required this.time});
  final bool isBot;
  final String text;
  final DateTime time;

  factory _ChatBubble.fromJson(Map<String, dynamic> json) {
    return _ChatBubble(
      isBot: json['isBot'] as bool? ?? false,
      text: json['text'] as String? ?? '',
      time: DateTime.tryParse(json['time']?.toString() ?? '')?.toLocal() ??
          DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
        'isBot': isBot,
        'text': text,
        'time': time.toIso8601String(),
      };
}
