import 'package:ecommerce_mobile/core/components/app_back_button.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/models/app_notification.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class NotificationPage extends StatefulWidget {
  const NotificationPage({super.key});

  @override
  State<NotificationPage> createState() => _NotificationPageState();
}

class _NotificationPageState extends State<NotificationPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final auth = context.read<AuthProvider>();
      final notifications = context.read<NotificationProvider>();
      if (auth.isAuthenticated) {
        notifications.refresh();
        notifications.connectRealtime();
      }
    });
  }

  String _relativeTime(DateTime? utc) {
    if (utc == null) return '';
    final local = utc.toLocal();
    final diff = DateTime.now().difference(local);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes} min ago';
    if (diff.inHours < 24) return '${diff.inHours} h ago';
    if (diff.inDays < 7) return '${diff.inDays} d ago';
    return '${local.year}-${local.month.toString().padLeft(2, '0')}-${local.day.toString().padLeft(2, '0')}';
  }

  IconData _iconForType(String? type) {
    switch (type) {
      case 'Payment':
        return Icons.payments_outlined;
      case 'Cancellation':
        return Icons.event_busy_outlined;
      case 'Status':
        return Icons.flag_outlined;
      case 'Reservation':
        return Icons.confirmation_number_outlined;
      case 'Message':
        return Icons.chat_bubble_outline;
      case 'Email':
        return Icons.mail_outline;
      default:
        return Icons.notifications_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      appBar: AppBar(
        leading: const AppBackButton(),
        title: const Text('Notifications'),
        actions: [
          if (auth.isAuthenticated)
            TextButton(
              onPressed: () async {
                try {
                  await context.read<NotificationProvider>().markAllRead();
                } on Exception catch (e) {
                  if (mounted) alertBox(context, 'Error', e.toString());
                }
              },
              child: const Text('Mark all read'),
            ),
        ],
      ),
      body: !auth.isAuthenticated
          ? const Center(child: Text('Sign in to see your notifications.'))
          : Consumer<NotificationProvider>(
              builder: (context, provider, _) {
                if (provider.loading && provider.items.isEmpty) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (provider.items.isEmpty) {
                  return RefreshIndicator(
                    onRefresh: provider.refresh,
                    child: ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 120),
                        Center(child: Text('No notifications yet.')),
                      ],
                    ),
                  );
                }

                return RefreshIndicator(
                  onRefresh: provider.refresh,
                  child: ListView.builder(
                    padding: const EdgeInsets.only(top: AppDefaults.padding),
                    itemCount: provider.items.length,
                    itemBuilder: (context, index) {
                      final item = provider.items[index];
                      return _NotificationTile(
                        item: item,
                        time: _relativeTime(item.createdAt),
                        icon: _iconForType(item.type),
                        onTap: () async {
                          if (item.id != null && item.isRead != true) {
                            try {
                              await provider.markAsRead(item.id!);
                            } on Exception catch (e) {
                              if (mounted) {
                                alertBox(context, 'Error', e.toString());
                              }
                            }
                          }
                        },
                      );
                    },
                  ),
                );
              },
            ),
    );
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({
    required this.item,
    required this.time,
    required this.icon,
    required this.onTap,
  });

  final AppNotification item;
  final String time;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final unread = item.isRead != true;
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        child: Column(
          children: [
            ListTile(
              leading: CircleAvatar(
                backgroundColor: unread
                    ? Theme.of(context).colorScheme.primaryContainer
                    : Colors.grey.shade200,
                child: Icon(icon, size: 20),
              ),
              title: Text(
                item.title ?? '',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      fontWeight: unread ? FontWeight.bold : FontWeight.w500,
                    ),
              ),
              subtitle: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(height: 4),
                  Text(item.message ?? ''),
                  const SizedBox(height: 4),
                  Text(
                    [
                      if (item.type != null && item.type!.isNotEmpty) item.type!,
                      time,
                    ].where((e) => e.isNotEmpty).join(' · '),
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            const Padding(
              padding: EdgeInsets.only(left: 72),
              child: Divider(thickness: 0.1),
            ),
          ],
        ),
      ),
    );
  }
}
