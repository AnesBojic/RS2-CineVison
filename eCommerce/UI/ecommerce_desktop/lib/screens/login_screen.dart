import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/layouts/home_shell.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _loading = false;
  bool _obscure = true;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Row(
        children: [
          Expanded(
            flex: 5,
            child: Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [Color(0xFF1A0508), Color(0xFF0A0E14), Color(0xFF0A0E14)],
                ),
              ),
              child: Stack(
                children: [
                  Positioned(
                    top: -80,
                    left: -80,
                    child: Container(
                      width: 260,
                      height: 260,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: AppColors.primary.withValues(alpha: 0.08),
                      ),
                    ),
                  ),
                  Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 72,
                          height: 72,
                          decoration: BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.circular(16),
                            boxShadow: [
                              BoxShadow(
                                color: AppColors.primary.withValues(alpha: 0.4),
                                blurRadius: 24,
                                offset: const Offset(0, 8),
                              ),
                            ],
                          ),
                          child: const Icon(Icons.local_movies_rounded, color: Colors.white, size: 36),
                        ),
                        const SizedBox(height: 24),
                        const Text(
                          'CINEVISION',
                          style: TextStyle(
                            color: AppColors.textPrimary,
                            fontSize: 36,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 4,
                          ),
                        ),
                        const SizedBox(height: 12),
                        const Text(
                          'Cinema Management System',
                          style: TextStyle(color: AppColors.textSecondary, fontSize: 16),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            flex: 4,
            child: Center(
              child: Container(
                constraints: const BoxConstraints(maxWidth: 400),
                padding: const EdgeInsets.symmetric(horizontal: 40),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Welcome back',
                      style: TextStyle(
                        color: AppColors.textPrimary,
                        fontSize: 28,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Sign in to manage your cinema',
                      style: TextStyle(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: 36),
                    TextField(
                      controller: _usernameController,
                      decoration: const InputDecoration(
                        labelText: 'Username',
                        prefixIcon: Icon(Icons.person_outline, size: 20),
                      ),
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      controller: _passwordController,
                      obscureText: _obscure,
                      onSubmitted: (_) => _login(),
                      decoration: InputDecoration(
                        labelText: 'Password',
                        prefixIcon: const Icon(Icons.lock_outline, size: 20),
                        suffixIcon: IconButton(
                          onPressed: () => setState(() => _obscure = !_obscure),
                          icon: Icon(
                            _obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                            size: 20,
                          ),
                        ),
                      ),
                    ),
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton(
                        onPressed: _loading ? null : _showForgotPassword,
                        child: const Text('Forgot password?'),
                      ),
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      height: 48,
                      child: ElevatedButton(
                        onPressed: _loading ? null : _login,
                        child: _loading
                            ? const SizedBox(
                                width: 22,
                                height: 22,
                                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                              )
                            : const Text('Sign In'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _login() async {
    setState(() => _loading = true);
    try {
      await context.read<AuthProvider>().login(
            _usernameController.text.trim(),
            _passwordController.text,
          );
      if (!mounted) return;
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const HomeShell()),
      );
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Login failed', e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _showForgotPassword() async {
    final accountCtrl = TextEditingController(text: _usernameController.text.trim());
    final codeCtrl = TextEditingController();
    final passwordCtrl = TextEditingController();
    final confirmCtrl = TextEditingController();
    var step = 0;
    var busy = false;
    var obscure = true;

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) {
          Future<void> sendCode() async {
            if (accountCtrl.text.trim().isEmpty) {
              alertBox(context, 'Validation', 'Email or username is required');
              return;
            }
            setDialogState(() => busy = true);
            try {
              final message = await this.context.read<AuthProvider>().forgotPassword(
                    accountCtrl.text.trim(),
                  );
              if (!context.mounted) return;
              showAppSnackBar(this.context, message);
              setDialogState(() {
                step = 1;
                busy = false;
              });
            } on Exception catch (e) {
              setDialogState(() => busy = false);
              if (context.mounted) alertBox(context, 'Reset password', e.toString());
            }
          }

          Future<void> resetPassword() async {
            if (codeCtrl.text.trim().length != 6) {
              alertBox(context, 'Validation', 'Enter the 6-digit code from your email');
              return;
            }
            if (passwordCtrl.text.length < 6) {
              alertBox(context, 'Validation', 'Password must be at least 6 characters');
              return;
            }
            if (passwordCtrl.text != confirmCtrl.text) {
              alertBox(context, 'Validation', 'Passwords do not match');
              return;
            }
            setDialogState(() => busy = true);
            try {
              final message = await this.context.read<AuthProvider>().resetPassword(
                    emailOrUsername: accountCtrl.text.trim(),
                    code: codeCtrl.text.trim(),
                    newPassword: passwordCtrl.text,
                    confirmPassword: confirmCtrl.text,
                  );
              if (!context.mounted) return;
              Navigator.pop(context);
              showAppSnackBar(this.context, message);
            } on Exception catch (e) {
              setDialogState(() => busy = false);
              if (context.mounted) alertBox(context, 'Reset password', e.toString());
            }
          }

          return AlertDialog(
            backgroundColor: AppColors.card,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
              side: const BorderSide(color: AppColors.cardBorder),
            ),
            title: Text(
              step == 0 ? 'Forgot password' : 'Set new password',
              style: const TextStyle(color: AppColors.textPrimary),
            ),
            content: SizedBox(
              width: 420,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    step == 0
                        ? 'Enter your email or username. A 6-digit code will be sent to the account email.'
                        : 'Enter the code from your email and choose a new password.',
                    style: const TextStyle(color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: accountCtrl,
                    readOnly: step == 1,
                    decoration: const InputDecoration(labelText: 'Email or username'),
                  ),
                  if (step == 1) ...[
                    const SizedBox(height: 12),
                    TextField(
                      controller: codeCtrl,
                      decoration: const InputDecoration(labelText: 'Reset code'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: passwordCtrl,
                      obscureText: obscure,
                      decoration: InputDecoration(
                        labelText: 'New password',
                        suffixIcon: IconButton(
                          onPressed: () => setDialogState(() => obscure = !obscure),
                          icon: Icon(
                            obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: confirmCtrl,
                      obscureText: obscure,
                      decoration: const InputDecoration(labelText: 'Confirm password'),
                    ),
                  ],
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: busy ? null : () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              if (step == 1)
                TextButton(
                  onPressed: busy
                      ? null
                      : () => setDialogState(() {
                            step = 0;
                          }),
                  child: const Text('Back'),
                ),
              ElevatedButton(
                onPressed: busy ? null : (step == 0 ? sendCode : resetPassword),
                child: busy
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(step == 0 ? 'Send code' : 'Reset password'),
              ),
            ],
          );
        },
      ),
    );

    accountCtrl.dispose();
    codeCtrl.dispose();
    passwordCtrl.dispose();
    confirmCtrl.dispose();
  }
}
