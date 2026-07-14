import 'dart:convert';

import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:flutter/material.dart';

class ProfileAvatar extends StatelessWidget {
  const ProfileAvatar({
    super.key,
    this.profileImageBase64,
    this.displayName,
    this.radius = 18,
    this.onTap,
  });

  final String? profileImageBase64;
  final String? displayName;
  final double radius;
  final VoidCallback? onTap;

  String get _initials {
    final name = (displayName ?? '').trim();
    if (name.isEmpty) return '?';
    final parts = name.split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
    if (parts.length >= 2) {
      return '${parts.first[0]}${parts[1][0]}'.toUpperCase();
    }
    return parts.first[0].toUpperCase();
  }

  ImageProvider? _imageProvider() {
    final raw = profileImageBase64;
    if (raw == null || raw.isEmpty) return null;
    try {
      final cleaned = raw.contains(',') ? raw.split(',').last : raw;
      return MemoryImage(base64Decode(cleaned));
    } catch (_) {
      return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final image = _imageProvider();

    final avatar = CircleAvatar(
      radius: radius,
      backgroundColor: AppColors.gray,
      backgroundImage: image,
      child: image == null
          ? Text(
              _initials,
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: radius * 0.85,
                fontWeight: FontWeight.w600,
              ),
            )
          : null,
    );

    if (onTap == null) return avatar;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: avatar,
      ),
    );
  }
}
