import 'dart:convert';

import 'package:ecommerce_desktop/core/enums/api_enums.dart';
import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/models/notification.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/notification_provider.dart';
import 'package:ecommerce_desktop/screens/profile_screen.dart';
import 'package:ecommerce_desktop/utils/base64_image_cache.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

class TopBar extends StatelessWidget {
  const TopBar({super.key, required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final notifications = context.watch<NotificationProvider>();
    final todayLabel = DateFormat('MMM d, yyyy').format(DateTime.now());

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 18),
      decoration: const BoxDecoration(
        color: AppColors.background,
      ),
      child: Row(
        children: [
          Text(
            title,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 24,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Spacer(),
          Text(todayLabel, style: const TextStyle(color: AppColors.textSecondary, fontSize: 14)),
          const SizedBox(width: 22),
          InkWell(
            borderRadius: BorderRadius.circular(8),
            onTap: () => _showNotifications(context),
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                const Padding(
                  padding: EdgeInsets.all(4),
                  child: Icon(Icons.notifications_outlined, color: AppColors.textSecondary, size: 22),
                ),
                if (notifications.unreadCount > 0)
                  Positioned(
                    right: 0,
                    top: 0,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      constraints: const BoxConstraints(minWidth: 18, minHeight: 18),
                      child: Text(
                        notifications.unreadCount > 99 ? '99+' : '${notifications.unreadCount}',
                        textAlign: TextAlign.center,
                        style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: 22),
          InkWell(
            borderRadius: BorderRadius.circular(20),
            onTap: () => showProfileDialog(context),
            child: CircleAvatar(
              key: ValueKey(auth.profileImageBase64?.hashCode ?? 0),
              radius: 18,
              backgroundColor: AppColors.inputFill,
              backgroundImage: () {
                final bytes = Base64ImageCache.decode(auth.profileImageBase64);
                return bytes != null ? MemoryImage(bytes) : null;
              }(),
              child: Base64ImageCache.decode(auth.profileImageBase64) == null
                  ? Text(
                      _initials(auth.displayName),
                      style: const TextStyle(color: AppColors.textPrimary, fontSize: 12, fontWeight: FontWeight.w600),
                    )
                  : null,
            ),
          ),
        ],
      ),
    );
  }

  void _showNotifications(BuildContext context) async {
    final provider = context.read<NotificationProvider>();
    await provider.refresh();
    if (!context.mounted) return;

    showDialog(
      context: context,
      builder: (dialogContext) => Consumer<NotificationProvider>(
        builder: (context, provider, _) => AlertDialog(
        backgroundColor: AppColors.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            const Expanded(
              child: Text('Notifications', style: TextStyle(color: AppColors.textPrimary)),
            ),
            if (provider.unreadCount > 0)
              TextButton(
                onPressed: () async {
                  await provider.markAllRead();
                  if (dialogContext.mounted) Navigator.pop(dialogContext);
                },
                child: const Text('Mark all read'),
              ),
          ],
        ),
        content: SizedBox(
          width: 420,
          child: provider.items.isEmpty
              ? const Padding(
                  padding: EdgeInsets.symmetric(vertical: 24),
                  child: Center(
                    child: Text('No notifications yet', style: TextStyle(color: AppColors.textSecondary)),
                  ),
                )
              : SizedBox(
                  height: 360,
                  child: Scrollbar(
                    thumbVisibility: true,
                    child: ListView.separated(
                      itemCount: provider.items.length,
                      separatorBuilder: (_, __) => Divider(color: AppColors.divider, height: 1),
                      itemBuilder: (context, index) {
                        final n = provider.items[index];
                        return _NotificationTile(notification: n);
                      },
                    ),
                  ),
                ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext), child: const Text('Close')),
        ],
      ),
      ),
    );
  }

  String _initials(String? name) {
    if (name == null || name.isEmpty) return 'U';
    final parts = name.trim().split(' ');
    if (parts.length >= 2) return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    return parts[0][0].toUpperCase();
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.notification});

  final AppNotification notification;

  @override
  Widget build(BuildContext context) {
    final isEmail = (notification.type ?? '').toLowerCase() ==
        NotificationTypes.email.toLowerCase();
    final icon = isEmail ? Icons.mail_outline : Icons.chat_bubble_outline;
    final color = isEmail ? AppColors.blue : AppColors.green;
    final time = notification.createdAt != null
        ? DateFormat('MMM d, h:mm a').format(notification.createdAt!.toLocal())
        : '';

    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(icon, color: color, size: 18),
      ),
      title: Text(
        notification.title ?? 'Notification',
        style: TextStyle(
          color: AppColors.textPrimary,
          fontWeight: notification.isRead == true ? FontWeight.w500 : FontWeight.w700,
        ),
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (notification.message?.isNotEmpty == true)
            Text(notification.message!, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
          if (time.isNotEmpty)
            Text(time, style: const TextStyle(color: AppColors.textSecondary, fontSize: 11)),
        ],
      ),
      onTap: () async {
        if (notification.id != null && notification.isRead != true) {
          await context.read<NotificationProvider>().markAsRead(notification.id!);
        }
      },
    );
  }
}

class ManagePageLayout extends StatelessWidget {
  const ManagePageLayout({
    super.key,
    required this.title,
    required this.child,
    this.toolbar,
    this.isLoading = false,
  });

  final String title;
  final Widget? toolbar;
  final Widget child;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(32, 24, 32, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Text(
                title,
                style: const TextStyle(
                  color: AppColors.textPrimary,
                  fontSize: 22,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const Spacer(),
              if (toolbar != null) toolbar!,
            ],
          ),
          const SizedBox(height: 20),
          Expanded(
            child: Stack(
              children: [
                Positioned.fill(child: child),
                if (isLoading)
                  Positioned.fill(
                    child: Container(
                      color: AppColors.background.withValues(alpha: 0.55),
                      child: const Center(
                        child: CircularProgressIndicator(color: AppColors.primary),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class SearchField extends StatelessWidget {
  const SearchField({
    super.key,
    required this.controller,
    required this.hint,
    this.width = 260,
    this.onSubmitted,
    this.onChanged,
  });

  final TextEditingController controller;
  final String hint;
  final double width;
  final ValueChanged<String>? onSubmitted;
  final ValueChanged<String>? onChanged;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: width,
      height: 44,
      child: TextField(
        controller: controller,
        onSubmitted: onSubmitted,
        onChanged: onChanged,
        style: const TextStyle(color: AppColors.textPrimary, fontSize: 14),
        decoration: InputDecoration(
          hintText: hint,
          contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          prefixIcon: const Icon(Icons.search, color: AppColors.textSecondary, size: 20),
          isDense: true,
        ),
      ),
    );
  }
}

class FilterDropdown extends StatelessWidget {
  const FilterDropdown({
    super.key,
    required this.hint,
    required this.value,
    required this.items,
    required this.onChanged,
  });

  final String hint;
  final String? value;
  final List<DropdownMenuItem<String?>> items;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 44,
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        color: AppColors.inputFill,
        borderRadius: BorderRadius.circular(10),
      ),
      child: DropdownButton<String?>(
        value: value,
        hint: Text(hint, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
        underline: const SizedBox(),
        dropdownColor: AppColors.card,
        icon: const Icon(Icons.keyboard_arrow_down, color: AppColors.textSecondary, size: 20),
        items: items,
        onChanged: onChanged,
      ),
    );
  }
}

class PrimaryButton extends StatelessWidget {
  const PrimaryButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.compact = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: compact ? 40 : 44,
      child: ElevatedButton.icon(
        onPressed: onPressed,
        icon: Icon(icon ?? Icons.add, size: 18),
        label: Text(label),
        style: ElevatedButton.styleFrom(
          padding: EdgeInsets.symmetric(horizontal: compact ? 14 : 18, vertical: 0),
        ),
      ),
    );
  }
}

class SecondaryButton extends StatelessWidget {
  const SecondaryButton({super.key, required this.label, required this.onPressed});

  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 44,
      child: OutlinedButton(
        onPressed: onPressed,
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.textSecondary,
          side: BorderSide(color: AppColors.cardBorder.withValues(alpha: 0.4)),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
        child: Text(label),
      ),
    );
  }
}

class StatusBadge extends StatelessWidget {
  const StatusBadge({super.key, required this.label, required this.color, this.filled = false});

  final String label;
  final Color color;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: filled ? color.withValues(alpha: 0.16) : color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class RoleBadge extends StatelessWidget {
  const RoleBadge({super.key, required this.role});

  final String role;

  Color get _color {
    switch (role.toLowerCase()) {
      case 'admin':
        return AppColors.purple;
      case 'staff':
        return AppColors.blue;
      default:
        return AppColors.green;
    }
  }

  @override
  Widget build(BuildContext context) {
    return StatusBadge(label: role, color: _color, filled: true);
  }
}

class StatCard extends StatelessWidget {
  const StatCard({
    super.key,
    required this.icon,
    required this.iconColor,
    required this.value,
    required this.label,
    required this.subtitle,
  });

  final IconData icon;
  final Color iconColor;
  final String value;
  final String label;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: AppDecorations.card(radius: 14),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: iconColor.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: iconColor, size: 22),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    value,
                    style: const TextStyle(
                      color: AppColors.textPrimary,
                      fontSize: 28,
                      fontWeight: FontWeight.w800,
                      height: 1.1,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(label, style: const TextStyle(color: AppColors.textPrimary, fontSize: 13)),
                  Text(subtitle, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class DataCard extends StatelessWidget {
  const DataCard({super.key, required this.child, this.emptyMessage});

  final Widget child;
  final String? emptyMessage;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      decoration: AppDecorations.card(radius: 14),
      child: emptyMessage != null
          ? Padding(
              padding: const EdgeInsets.all(48),
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.inbox_outlined, color: AppColors.textSecondary, size: 40),
                    const SizedBox(height: 12),
                    Text(emptyMessage!, style: const TextStyle(color: AppColors.textSecondary)),
                  ],
                ),
              ),
            )
          : ClipRRect(
              borderRadius: BorderRadius.circular(14),
              child: child,
            ),
    );
  }
}

class SectionHeader extends StatelessWidget {
  const SectionHeader({super.key, required this.title, this.action});

  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Row(
        children: [
          Text(
            title,
            style: const TextStyle(
              color: AppColors.textPrimary,
              fontSize: 17,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Spacer(),
          if (action != null) action!,
        ],
      ),
    );
  }
}

class ActionIconButton extends StatelessWidget {
  const ActionIconButton({
    super.key,
    required this.icon,
    required this.color,
    required this.onPressed,
    this.tooltip,
    this.enabled = true,
  });

  final IconData icon;
  final Color color;
  final VoidCallback onPressed;
  final String? tooltip;

  /// When false the button is greyed out and ignores taps. Pair it with a
  /// [tooltip] that explains why the action is unavailable.
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final effectiveColor = enabled ? color : AppColors.textSecondary;
    final button = Material(
      color: effectiveColor.withValues(alpha: enabled ? 0.12 : 0.06),
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: enabled ? onPressed : null,
        child: SizedBox(
          width: 34,
          height: 34,
          child: Icon(
            icon,
            color: enabled ? effectiveColor : effectiveColor.withValues(alpha: 0.5),
            size: 18,
          ),
        ),
      ),
    );
    if (tooltip == null || tooltip!.isEmpty) return button;
    return Tooltip(message: tooltip!, child: button);
  }
}

/// Chip used to switch between sections inside a single page (e.g. reference data).
class SectionChip extends StatelessWidget {
  const SectionChip({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? AppColors.primary : AppColors.inputFill,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        borderRadius: BorderRadius.circular(10),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Text(
            label,
            style: TextStyle(
              color: selected ? Colors.white : AppColors.textSecondary,
              fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
              fontSize: 13,
            ),
          ),
        ),
      ),
    );
  }
}

/// Right-aligned actions header for data tables.
const DataColumn actionsDataColumn = DataColumn(
  label: Align(
    alignment: Alignment.centerRight,
    child: Text('Actions'),
  ),
);

/// Right-aligns edit/delete buttons inside a wide Actions cell.
DataCell actionButtonsCell(List<Widget> buttons) {
  final children = <Widget>[];
  for (var i = 0; i < buttons.length; i++) {
    if (i > 0) children.add(const SizedBox(width: 8));
    children.add(buttons[i]);
  }
  return DataCell(
    Align(
      alignment: Alignment.centerRight,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.end,
        children: children,
      ),
    ),
  );
}

class StyledDataTable extends StatefulWidget {
  const StyledDataTable({
    super.key,
    required this.columns,
    required this.rows,
  });

  final List<DataColumn> columns;
  final List<DataRow> rows;

  @override
  State<StyledDataTable> createState() => _StyledDataTableState();
}

class _StyledDataTableState extends State<StyledDataTable> {
  final _horizontalController = ScrollController();
  final _verticalController = ScrollController();

  @override
  void dispose() {
    _horizontalController.dispose();
    _verticalController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scrollbar(
      controller: _verticalController,
      thumbVisibility: true,
      child: SingleChildScrollView(
        controller: _verticalController,
        child: Scrollbar(
          controller: _horizontalController,
          thumbVisibility: true,
          notificationPredicate: (notification) => notification.depth == 1,
          child: SingleChildScrollView(
            controller: _horizontalController,
            scrollDirection: Axis.horizontal,
            child: ConstrainedBox(
              constraints: BoxConstraints(
                minWidth: (MediaQuery.sizeOf(context).width - 320).clamp(900, double.infinity),
              ),
              child: DividerTheme(
                data: const DividerThemeData(
                  color: AppColors.tableDivider,
                  thickness: 0.5,
                  space: 0,
                ),
                child: DataTable(
                  key: ValueKey(widget.rows.length),
                  columns: widget.columns,
                  rows: widget.rows,
                  dividerThickness: 0.5,
                  showBottomBorder: false,
                  columnSpacing: 24,
                  horizontalMargin: 16,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class FormDialogShell extends StatefulWidget {
  const FormDialogShell({
    super.key,
    required this.title,
    required this.child,
    required this.submitLabel,
    required this.onSubmit,
    this.isSubmitting = false,
    this.maxWidth = 580,
  });

  final String title;
  final Widget child;
  final String submitLabel;
  final VoidCallback onSubmit;
  final bool isSubmitting;
  final double maxWidth;

  @override
  State<FormDialogShell> createState() => _FormDialogShellState();
}

class _FormDialogShellState extends State<FormDialogShell> {
  final _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final maxHeight = MediaQuery.sizeOf(context).height * 0.85;

    return Dialog(
      backgroundColor: AppColors.card,
      insetPadding: const EdgeInsets.all(32),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: widget.maxWidth, maxHeight: maxHeight),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 20, 24, 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      widget.title,
                      style: const TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.close, color: AppColors.textSecondary, size: 20),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Expanded(
                child: Scrollbar(
                  controller: _scrollController,
                  thumbVisibility: true,
                  child: SingleChildScrollView(
                    controller: _scrollController,
                    child: widget.child,
                  ),
                ),
              ),
              const SizedBox(height: 24),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  SecondaryButton(label: 'Cancel', onPressed: () => Navigator.pop(context)),
                  const SizedBox(width: 12),
                  SizedBox(
                    height: 44,
                    child: ElevatedButton(
                      onPressed: widget.isSubmitting ? null : widget.onSubmit,
                      child: widget.isSubmitting
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : Text(widget.submitLabel),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class PosterUploadBox extends StatelessWidget {
  const PosterUploadBox({super.key, this.base64, required this.onPick});

  final String? base64;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onPick,
      child: Container(
        height: 130,
        width: double.infinity,
        decoration: BoxDecoration(
          color: AppColors.inputFill,
          borderRadius: BorderRadius.circular(12),
        ),
        child: base64 != null && base64!.isNotEmpty
            ? ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Image.memory(base64Decode(base64!), fit: BoxFit.cover),
              )
            : CustomPaint(
                painter: _DashedBorderPainter(color: AppColors.cardBorder.withValues(alpha: 0.45)),
                child: const Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.cloud_upload_outlined, color: AppColors.textSecondary, size: 28),
                      SizedBox(height: 8),
                      Text('Click to upload poster', style: TextStyle(color: AppColors.textSecondary)),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}

class _DashedBorderPainter extends CustomPainter {
  _DashedBorderPainter({required this.color});
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5;
    const dash = 6.0;
    const gap = 4.0;
    final path = Path()
      ..addRRect(RRect.fromRectAndRadius(
        Rect.fromLTWH(1, 1, size.width - 2, size.height - 2),
        const Radius.circular(11),
      ));
    for (final metric in path.computeMetrics()) {
      var distance = 0.0;
      while (distance < metric.length) {
        final next = distance + dash;
        canvas.drawPath(metric.extractPath(distance, next.clamp(0, metric.length)), paint);
        distance = next + gap;
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class RatingChip extends StatelessWidget {
  const RatingChip({super.key, required this.rating});

  final double? rating;

  @override
  Widget build(BuildContext context) {
    if (rating == null) return const Text('—');
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: Colors.amber.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: Colors.amber.withValues(alpha: 0.35)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.star_rounded, color: Colors.amber, size: 16),
          const SizedBox(width: 4),
          Text(
            rating!.toStringAsFixed(1),
            style: const TextStyle(color: Colors.amber, fontWeight: FontWeight.w600, fontSize: 12),
          ),
        ],
      ),
    );
  }
}

Widget posterThumbnail(String? base64, {double size = 36}) {
  if (base64 != null && base64.isNotEmpty) {
    final bytes = Base64ImageCache.decode(base64);
    if (bytes != null) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Image.memory(
          bytes,
          width: size,
          height: size,
          fit: BoxFit.cover,
          gaplessPlayback: true,
        ),
      );
    }
  }
  return Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      color: AppColors.inputFill,
      borderRadius: BorderRadius.circular(8),
      border: Border.all(color: AppColors.cardBorder),
    ),
    child: const Icon(Icons.movie_outlined, color: AppColors.textSecondary, size: 18),
  );
}

Widget occupancyBar(double percent, Color color, {double width = 120}) {
  return SizedBox(
    width: width,
    child: Row(
      children: [
        Expanded(
          child: ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: (percent / 100).clamp(0, 1),
              minHeight: 8,
              backgroundColor: AppColors.inputFill,
              color: color,
            ),
          ),
        ),
        const SizedBox(width: 8),
        Text('${percent.toStringAsFixed(0)}%', style: const TextStyle(fontSize: 13)),
      ],
    ),
  );
}

String formatDate(DateTime? date) {
  if (date == null) return '—';
  return DateFormat('MMM d, yyyy').format(date.toLocal());
}

String formatTime(DateTime? date) {
  if (date == null) return '—';
  return DateFormat('h:mm a').format(date.toLocal());
}

String formatCurrency(num? value) {
  if (value == null) return '—';
  return '\$${value.toStringAsFixed(value == value.roundToDouble() ? 0 : 2)}';
}

Future<bool?> confirmDelete(BuildContext context, String message) {
  return showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      backgroundColor: AppColors.card,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      title: const Row(
        children: [
          Icon(Icons.warning_amber_rounded, color: AppColors.orange, size: 22),
          SizedBox(width: 10),
          Text('Delete', style: TextStyle(color: AppColors.textPrimary)),
        ],
      ),
      content: Text(message, style: const TextStyle(color: AppColors.textSecondary)),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
        ElevatedButton(onPressed: () => Navigator.pop(context, true), child: const Text('Delete')),
      ],
    ),
  );
}

/// Builds a cascade-delete warning from a `DeleteImpact` API response.
String buildCascadeDeleteWarning({
  required String subjectLabel,
  Map<String, dynamic>? impact,
}) {
  final buffer = StringBuffer();
  buffer.writeln('Delete $subjectLabel?');
  buffer.writeln();

  final total = impact?['totalAffectedRows'] as int? ?? 0;
  final items = impact?['items'];
  if (total > 0 && items is List && items.isNotEmpty) {
    buffer.writeln(
      'Warning: this will also permanently remove $total related record(s):',
    );
    for (final item in items) {
      if (item is! Map) continue;
      final name = item['entityName']?.toString() ?? 'Related';
      final count = item['count'] as int? ?? 0;
      if (count > 0) {
        buffer.writeln('• $count $name');
      }
    }
  } else {
    buffer.writeln(
      'No related bookings were found. This will still be removed permanently.',
    );
  }

  return buffer.toString().trim();
}
