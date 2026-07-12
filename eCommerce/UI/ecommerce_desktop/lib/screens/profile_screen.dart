import 'dart:convert';

import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/user.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/image_utils.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

Future<void> showProfileDialog(BuildContext context) async {
  final userProvider = context.read<UserProvider>();
  final auth = context.read<AuthProvider>();

  User? profile;
  try {
    profile = await userProvider.getMe();
  } on Exception catch (e) {
    if (context.mounted) alertBox(context, 'Error', e.toString());
    return;
  }

  if (!context.mounted) return;

  final loadedProfile = profile;
  final firstCtrl = TextEditingController(text: loadedProfile.firstName ?? '');
  final lastCtrl = TextEditingController(text: loadedProfile.lastName ?? '');
  final emailCtrl = TextEditingController(text: loadedProfile.email ?? '');
  final phoneCtrl = TextEditingController(text: loadedProfile.phoneNumber ?? '');
  final currentPwdCtrl = TextEditingController();
  final newPwdCtrl = TextEditingController();
  final confirmPwdCtrl = TextEditingController();
  String? profileImageBase64 = loadedProfile.profileImageBase64;
  bool showPasswordFields = false;
  bool submitting = false;

  await showDialog(
    context: context,
    builder: (dialogContext) => StatefulBuilder(
      builder: (context, setDialogState) => FormDialogShell(
        title: 'My Profile',
        submitLabel: 'Save Profile',
        isSubmitting: submitting,
        maxWidth: 560,
        onSubmit: () async {
          setDialogState(() => submitting = true);
          try {
            await userProvider.updateMe({
              'firstName': firstCtrl.text.trim(),
              'lastName': lastCtrl.text.trim(),
              'email': emailCtrl.text.trim(),
              'phoneNumber': phoneCtrl.text.trim(),
              if (profileImageBase64 != null) 'profileImageBase64': profileImageBase64,
            });

            final refreshed = await userProvider.getMe();
            final savedImage = refreshed.profileImageBase64 ?? profileImageBase64;

            if (showPasswordFields &&
                currentPwdCtrl.text.isNotEmpty &&
                newPwdCtrl.text.isNotEmpty) {
              final userId = auth.userId ?? refreshed.id;
              if (userId == null) {
                throw Exception('Could not determine user id for password change.');
              }
              await userProvider.changePassword(
                userId: userId,
                currentPassword: currentPwdCtrl.text,
                newPassword: newPwdCtrl.text,
                confirmPassword: confirmPwdCtrl.text,
              );
            }

            auth.updateFromProfile(
              firstName: refreshed.firstName,
              lastName: refreshed.lastName,
              email: refreshed.email,
              profileImageBase64: savedImage,
            );

            if (context.mounted) {
              Navigator.pop(context);
              showAppSnackBar(
                dialogContext,
                showPasswordFields && newPwdCtrl.text.isNotEmpty
                    ? 'Profile and password updated'
                    : 'Profile updated',
              );
            }
          } on ApiClientException catch (e) {
            setDialogState(() => submitting = false);
            if (context.mounted) {
              showAppSnackBar(context, e.message, isError: true);
            }
          } on Exception catch (e) {
            setDialogState(() => submitting = false);
            if (context.mounted) alertBox(context, 'Error', e.toString());
          }
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Column(
                children: [
                  GestureDetector(
                    onTap: () async {
                      final result = await FilePicker.pickFiles(
                        type: FileType.image,
                        withData: true,
                      );
                      if (result != null && result.files.single.bytes != null) {
                        final compressed =
                            await preparePosterBase64(result.files.single.bytes!);
                        setDialogState(() => profileImageBase64 = compressed);
                      }
                    },
                    child: CircleAvatar(
                      key: ValueKey(profileImageBase64?.hashCode ?? 0),
                      radius: 42,
                      backgroundColor: AppColors.inputFill,
                      backgroundImage: profileImageBase64 != null &&
                              profileImageBase64!.isNotEmpty
                          ? MemoryImage(
                              base64Decode(profileImageBase64!),
                            )
                          : null,
                      child: profileImageBase64 == null || profileImageBase64!.isEmpty
                          ? const Icon(Icons.person, size: 36, color: AppColors.textSecondary)
                          : null,
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: () async {
                      final result = await FilePicker.pickFiles(
                        type: FileType.image,
                        withData: true,
                      );
                      if (result != null && result.files.single.bytes != null) {
                        final compressed =
                            await preparePosterBase64(result.files.single.bytes!);
                        setDialogState(() => profileImageBase64 = compressed);
                      }
                    },
                    child: const Text('Change photo'),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(
                child: TextField(
                  controller: firstCtrl,
                  decoration: const InputDecoration(labelText: 'First Name'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextField(
                  controller: lastCtrl,
                  decoration: const InputDecoration(labelText: 'Last Name'),
                ),
              ),
            ]),
            const SizedBox(height: 12),
            TextField(
              controller: emailCtrl,
              decoration: const InputDecoration(labelText: 'Email'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: phoneCtrl,
              decoration: const InputDecoration(labelText: 'Phone Number'),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: false,
              controller: TextEditingController(text: loadedProfile.username ?? ''),
              decoration: const InputDecoration(
                labelText: 'Username',
                helperText: 'Username cannot be changed here',
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: false,
              controller: TextEditingController(
                text: (loadedProfile.role?.isNotEmpty == true) ? loadedProfile.role! : '—',
              ),
              decoration: const InputDecoration(
                labelText: 'Role',
                helperText: 'Contact an admin to change your role',
              ),
            ),
            const SizedBox(height: 16),
            TextButton.icon(
              onPressed: () => setDialogState(() => showPasswordFields = !showPasswordFields),
              icon: Icon(showPasswordFields ? Icons.expand_less : Icons.expand_more),
              label: Text(showPasswordFields ? 'Hide password change' : 'Change password'),
            ),
            if (showPasswordFields) ...[
              const SizedBox(height: 8),
              TextField(
                controller: currentPwdCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Current Password'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: newPwdCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'New Password'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: confirmPwdCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Confirm New Password'),
              ),
            ],
            const SizedBox(height: 8),
          ],
        ),
      ),
    ),
  );
}
