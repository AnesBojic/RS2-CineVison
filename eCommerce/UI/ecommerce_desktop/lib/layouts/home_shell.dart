import 'dart:convert';

import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/providers/analytics_provider.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/notification_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/screens/analytics_screen.dart';
import 'package:ecommerce_desktop/screens/chatbot_screen.dart';
import 'package:ecommerce_desktop/screens/dashboard_screen.dart';
import 'package:ecommerce_desktop/screens/hall_list_screen.dart';
import 'package:ecommerce_desktop/screens/login_screen.dart';
import 'package:ecommerce_desktop/screens/movie_list_screen.dart';
import 'package:ecommerce_desktop/screens/screening_list_screen.dart';
import 'package:ecommerce_desktop/screens/news_list_screen.dart';
import 'package:ecommerce_desktop/screens/profile_screen.dart';
import 'package:ecommerce_desktop/screens/user_list.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class HomeShell extends StatefulWidget {
  const HomeShell({super.key, this.initialIndex = 0});

  final int initialIndex;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  late int _selectedIndex;

  static const _allNavItems = [
    _NavItem('Dashboard', Icons.home_outlined, Icons.home, adminOnly: false),
    _NavItem('Movies', Icons.movie_outlined, Icons.movie, adminOnly: false),
    _NavItem('Halls', Icons.tv_outlined, Icons.tv, adminOnly: false),
    _NavItem('Projections', Icons.calendar_today_outlined, Icons.calendar_today, adminOnly: false),
    _NavItem('News', Icons.campaign_outlined, Icons.campaign, adminOnly: false),
    _NavItem('Users', Icons.people_outline, Icons.people, adminOnly: true),
    _NavItem('Analytics', Icons.bar_chart_outlined, Icons.bar_chart, adminOnly: false),
    _NavItem('Chatbot', Icons.chat_bubble_outline, Icons.chat_bubble, adminOnly: false),
  ];

  List<_NavItem> get _visibleNavItems {
    final auth = context.read<AuthProvider>();
    return _allNavItems.where((item) => !item.adminOnly || auth.isAdmin).toList();
  }

  @override
  void initState() {
    super.initState();
    _selectedIndex = widget.initialIndex;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadProfileSummary();
      context.read<NotificationProvider>().refresh();
      context.read<NotificationProvider>().connectRealtime();
      context.read<AnalyticsProvider>().connectRealtime();
    });
  }

  void _onNavTap(int index) {
    setState(() => _selectedIndex = index);
    if (index == 7) {
      context.read<NotificationProvider>().markAllRead(type: 'Message');
    } else {
      context.read<NotificationProvider>().refresh();
    }
  }

  Future<void> _loadProfileSummary() async {
    try {
      final user = await context.read<UserProvider>().getMe();
      if (!mounted) return;
      context.read<AuthProvider>().updateFromProfile(
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email,
        profileImageBase64: user.profileImageBase64,
      );
    } catch (_) {}
  }

  int? _movieEditId;
  int? _hallEditId;
  int? _screeningEditId;

  void _navigateTo(int index, {int? editId}) {
    setState(() {
      _selectedIndex = index;
      _movieEditId = index == 1 ? editId : null;
      _hallEditId = index == 2 ? editId : null;
      _screeningEditId = index == 3 ? editId : null;
    });
    if (index == 7) {
      context.read<NotificationProvider>().markAllRead(type: 'Message');
    } else {
      context.read<NotificationProvider>().refresh();
    }
  }

  void _clearEditId(int index) {
    setState(() {
      if (index == 1) _movieEditId = null;
      if (index == 2) _hallEditId = null;
      if (index == 3) _screeningEditId = null;
    });
  }

  Widget _screenForIndex(int index) {
    switch (index) {
      case 0:
        return DashboardScreen(onNavigate: _navigateTo);
      case 1:
        return MovieListScreen(
          editId: _movieEditId,
          onEditConsumed: () => _clearEditId(1),
        );
      case 2:
        return HallListScreen(
          editId: _hallEditId,
          onEditConsumed: () => _clearEditId(2),
        );
      case 3:
        return ScreeningListScreen(
          editId: _screeningEditId,
          onEditConsumed: () => _clearEditId(3),
        );
      case 4:
        return const NewsListScreen();
      case 5:
        return const UserList();
      case 6:
        return const AnalyticsScreen();
      case 7:
        return const ChatBotScreen();
      default:
        return DashboardScreen(onNavigate: _navigateTo);
    }
  }

  String _titleForIndex(int index) {
    if (index == 7) return 'Chatbot';
    return 'Dashboard';
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final visibleItems = _visibleNavItems;

    if (!visibleItems.any((item) => _allNavItems.indexOf(item) == _selectedIndex)) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) setState(() => _selectedIndex = 0);
      });
    }

    return Scaffold(
      backgroundColor: AppColors.background,
      body: Row(
        children: [
          _buildSidebar(auth, visibleItems),
          Expanded(
            child: Column(
              children: [
                TopBar(title: _titleForIndex(_selectedIndex)),
                Expanded(child: _screenForIndex(_selectedIndex)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSidebar(AuthProvider auth, List<_NavItem> visibleItems) {
    return Container(
      width: 250,
      decoration: const BoxDecoration(
        color: AppColors.sidebar,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 28),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 22),
            child: Row(
              children: [
                Container(
                  width: 38,
                  height: 38,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(10),
                    boxShadow: [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.35),
                        blurRadius: 12,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: const Icon(Icons.local_movies_rounded, color: Colors.white, size: 20),
                ),
                const SizedBox(width: 12),
                const Text(
                  'CINEVISION',
                  style: TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.4,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 28),
          ...visibleItems.map((item) {
            final index = _allNavItems.indexOf(item);
            final selected = _selectedIndex == index;
            return Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 3),
              child: Material(
                color: selected ? AppColors.primary : Colors.transparent,
                borderRadius: BorderRadius.circular(12),
                child: InkWell(
                  borderRadius: BorderRadius.circular(12),
                  hoverColor: selected ? null : AppColors.cardHover,
                  onTap: () => _onNavTap(index),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 13),
                    child: Row(
                      children: [
                        Icon(
                          selected ? item.activeIcon : item.icon,
                          color: selected ? Colors.white : AppColors.textSecondary,
                          size: 20,
                        ),
                        const SizedBox(width: 14),
                        Text(
                          item.label,
                          style: TextStyle(
                            color: selected ? Colors.white : AppColors.textSecondary,
                            fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
                            fontSize: 14,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            );
          }),
          const Spacer(),
          _buildUserFooter(auth),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 14),
            child: TextButton.icon(
              onPressed: () => _confirmLogout(context),
              icon: const Icon(Icons.logout, size: 18, color: AppColors.textSecondary),
              label: const Text('Logout', style: TextStyle(color: AppColors.textSecondary)),
              style: TextButton.styleFrom(
                alignment: Alignment.centerLeft,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              ),
            ),
          ),
          const SizedBox(height: 16),
        ],
      ),
    );
  }

  Widget _buildUserFooter(AuthProvider auth) {
    return InkWell(
      onTap: () => showProfileDialog(context),
      borderRadius: BorderRadius.circular(12),
      child: Container(
      margin: const EdgeInsets.symmetric(horizontal: 14),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.card,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          CircleAvatar(
            key: ValueKey(auth.profileImageBase64?.hashCode ?? 0),
            radius: 20,
            backgroundColor: AppColors.inputFill,
            backgroundImage: auth.profileImageBase64 != null &&
                    auth.profileImageBase64!.isNotEmpty
                ? MemoryImage(base64Decode(auth.profileImageBase64!))
                : null,
            child: auth.profileImageBase64 == null || auth.profileImageBase64!.isEmpty
                ? Text(
                    auth.displayName.isNotEmpty ? auth.displayName[0].toUpperCase() : 'U',
                    style: const TextStyle(color: AppColors.textPrimary, fontWeight: FontWeight.w700),
                  )
                : null,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  auth.displayName,
                  style: const TextStyle(
                    color: AppColors.textPrimary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
                Text(
                  auth.role ?? 'Staff',
                  style: const TextStyle(color: AppColors.textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          const Icon(Icons.edit_outlined, color: AppColors.textSecondary, size: 16),
        ],
      ),
    ),
    );
  }

  void _confirmLogout(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        title: const Text('Logout', style: TextStyle(color: AppColors.textPrimary)),
        content: const Text(
          'Are you sure you want to logout?',
          style: TextStyle(color: AppColors.textSecondary),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () {
              context.read<AnalyticsProvider>().disconnectRealtime();
              context.read<AuthProvider>().logout();
              Navigator.pushAndRemoveUntil(
                context,
                MaterialPageRoute(builder: (_) => const LoginScreen()),
                (route) => false,
              );
            },
            child: const Text('Logout'),
          ),
        ],
      ),
    );
  }
}

class _NavItem {
  const _NavItem(this.label, this.icon, this.activeIcon, {required this.adminOnly});
  final String label;
  final IconData icon;
  final IconData activeIcon;
  final bool adminOnly;
}
