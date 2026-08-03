import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';

/// CineVision branding header inspired by the desktop login screen.
class AuthBrandPanel extends StatelessWidget {
  const AuthBrandPanel({
    super.key,
    this.compact = false,
    this.subtitle = 'Book your cinema experience',
  });

  final bool compact;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    final height = compact ? 200.0 : 260.0;
    final logoSize = compact ? 56.0 : 72.0;
    final titleSize = compact ? 28.0 : 32.0;

    return Container(
      width: double.infinity,
      height: height,
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
            top: -60,
            left: -60,
            child: Container(
              width: 200,
              height: 200,
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
                  width: logoSize,
                  height: logoSize,
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
                  child: Icon(
                    Icons.local_movies_rounded,
                    color: Colors.white,
                    size: logoSize * 0.5,
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  'CINEVISION',
                  style: TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: titleSize,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 3,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  subtitle,
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 14,
                  ),
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
