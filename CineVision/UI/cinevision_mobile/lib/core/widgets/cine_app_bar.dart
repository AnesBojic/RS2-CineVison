import 'package:cinevision_mobile/core/constants/app_colors.dart';
import 'package:cinevision_mobile/core/routes/app_routes.dart';
import 'package:cinevision_mobile/core/widgets/profile_avatar.dart';
import 'package:cinevision_mobile/providers/auth_provider.dart';
import 'package:cinevision_mobile/providers/user_provider.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Consistent top bar: back arrow (when applicable) + auth action top-right.
class CineAppBar extends StatefulWidget implements PreferredSizeWidget {
  const CineAppBar({
    super.key,
    this.title,
    this.showBack,
    this.centerTitle = true,
    this.showAuthAction = true,
    this.additionalActions = const [],
  });

  final String? title;
  final bool? showBack;
  final bool centerTitle;
  final bool showAuthAction;
  final List<Widget> additionalActions;

  @override
  State<CineAppBar> createState() => _CineAppBarState();

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  static Future<void> logout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Log out'),
        content: const Text('Are you sure you want to log out?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Log out'),
          ),
        ],
      ),
    );
    if (confirmed == true && context.mounted) {
      final navigator = Navigator.of(context);
      await context.read<AuthProvider>().logout();
      navigator.pushNamedAndRemoveUntil(AppRoutes.authLanding, (_) => false);
    }
  }
}

class _CineAppBarState extends State<CineAppBar> {
  bool _profileSynced = false;
  bool? _wasAuthenticated;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final auth = context.read<AuthProvider>();
    if (_wasAuthenticated == true && !auth.isAuthenticated) {
      _profileSynced = false;
    }
    _wasAuthenticated = auth.isAuthenticated;
    _maybeSyncProfile();
  }

  Future<void> _maybeSyncProfile() async {
    if (_profileSynced) return;
    final auth = context.read<AuthProvider>();
    if (!auth.isAuthenticated) return;
    _profileSynced = true;
    await auth.syncProfileFromApi(context.read<UserProvider>());
  }

  void _goBack(BuildContext context) {
    if (Navigator.canPop(context)) {
      Navigator.pop(context);
      return;
    }
    final authed = context.read<AuthProvider>().isAuthenticated;
    Navigator.pushNamedAndRemoveUntil(
      context,
      authed ? AppRoutes.entryPoint : AppRoutes.authLanding,
      (_) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        final canPop = Navigator.canPop(context);
        final useBack = widget.showBack ?? canPop;

        return AppBar(
          automaticallyImplyLeading: false,
          leading: useBack
              ? IconButton(
                  icon: const Icon(Icons.arrow_back),
                  tooltip: 'Back',
                  onPressed: () => _goBack(context),
                )
              : null,
          title: widget.title != null ? Text(widget.title!) : null,
          centerTitle: widget.centerTitle,
          actions: [
            ...widget.additionalActions,
            if (widget.showAuthAction && auth.isAuthenticated)
              Padding(
                padding: const EdgeInsets.only(right: 4),
                child: ProfileAvatar(
                  profileImageBase64: auth.profileImageBase64,
                  displayName: auth.displayName,
                  onTap: () => Navigator.pushNamed(context, AppRoutes.myProfile),
                ),
              ),
            if (widget.showAuthAction)
              Padding(
                padding: const EdgeInsets.only(right: 8),
                child: auth.isAuthenticated
                    ? TextButton.icon(
                        onPressed: () => CineAppBar.logout(context),
                        icon: const Icon(Icons.logout, size: 18),
                        label: const Text('Log out'),
                        style: TextButton.styleFrom(
                          foregroundColor: AppColors.textPrimary,
                        ),
                      )
                    : TextButton.icon(
                        onPressed: () => Navigator.pushNamed(
                          context,
                          AppRoutes.authLanding,
                        ),
                        icon: const Icon(Icons.login, size: 18),
                        label: const Text('Sign In'),
                        style: TextButton.styleFrom(
                          foregroundColor: AppColors.primary,
                        ),
                      ),
              ),
          ],
        );
      },
    );
  }
}
