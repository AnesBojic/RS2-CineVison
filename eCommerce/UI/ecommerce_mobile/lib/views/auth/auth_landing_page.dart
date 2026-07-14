import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:flutter/material.dart';

import 'widgets/auth_brand_panel.dart';

/// Entry point for authentication — login or register only.
class AuthLandingPage extends StatelessWidget {
  const AuthLandingPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.scaffoldBackground,
      appBar: const CineAppBar(showBack: true, showAuthAction: false),
      body: SafeArea(
        top: false,
        child: Column(
          children: [
            const AuthBrandPanel(),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(AppDefaults.padding),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Spacer(),
                    Text(
                      'Welcome to CineVision',
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Sign in or create an account to book tickets.',
                      style: TextStyle(color: AppColors.textSecondary),
                      textAlign: TextAlign.center,
                    ),
                    const Spacer(),
                    SizedBox(
                      height: 48,
                      child: ElevatedButton(
                        onPressed: () => Navigator.pushNamed(context, AppRoutes.login),
                        child: const Text('Sign In'),
                      ),
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      height: 48,
                      child: OutlinedButton(
                        onPressed: () => Navigator.pushNamed(context, AppRoutes.signup),
                        child: const Text('Register'),
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
