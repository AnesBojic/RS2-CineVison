import 'dart:convert';

import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/user.dart';
import 'package:ecommerce_desktop/providers/notification_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/field_validators.dart';
import 'package:ecommerce_desktop/utils/image_utils.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class UserList extends StatefulWidget {
  const UserList({super.key});

  @override
  State<UserList> createState() => _UserListState();
}

class _UserListState extends State<UserList> {
  late UserProvider _userProvider;
  List<User> _users = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _userProvider = context.read<UserProvider>();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final filter = <String, dynamic>{'pageSize': 50};
      if (_searchController.text.isNotEmpty) filter['name'] = _searchController.text;
      final data = await _userProvider.get(filter: filter);
      if (!mounted) return;
      setState(() {
        _users = data.items ?? [];
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage Users',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search users...',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 10),
          PrimaryButton(label: 'Add User', onPressed: () => _showUserDialog()),
        ],
      ),
      child: DataCard(
        emptyMessage: _users.isEmpty ? 'No users found' : null,
        child: StyledDataTable(
          columns: const [
            DataColumn(label: Text('Name')),
            DataColumn(label: Text('Email')),
            DataColumn(label: Text('Role')),
            DataColumn(label: Text('Date Created')),
            DataColumn(label: Text('Date Modified')),
            DataColumn(label: Text('Actions')),
          ],
          rows: _users.map(_buildRow).toList(),
        ),
      ),
    );
  }

  DataRow _buildRow(User u) {
    final fullName = '${u.firstName ?? ''} ${u.lastName ?? ''}'.trim();
    return DataRow(cells: [
      DataCell(Row(children: [
        CircleAvatar(
          radius: 16,
          backgroundColor: AppColors.inputFill,
          child: Text(
            _initials(u.firstName, u.lastName),
            style: const TextStyle(fontSize: 11, color: AppColors.textSecondary, fontWeight: FontWeight.w600),
          ),
        ),
        const SizedBox(width: 12),
        Text(fullName.isEmpty ? '—' : fullName, style: const TextStyle(fontWeight: FontWeight.w500)),
      ])),
      DataCell(Text(u.email ?? '—')),
      DataCell(RoleBadge(role: (u.role?.isNotEmpty == true) ? u.role! : 'Customer')),
      DataCell(Text(formatDate(u.createdAt))),
      DataCell(Text(formatDate(u.updatedAt))),
      DataCell(Row(children: [
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          onPressed: () => _showUserDialog(user: u),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.mail_outline,
          color: AppColors.green,
          onPressed: () => _showEmailDialog(u),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          onPressed: () => _delete(u),
        ),
      ])),
    ]);
  }

  Future<void> _delete(User u) async {
    if (u.id == null) return;

    Map<String, dynamic>? impact;
    try {
      impact = await _userProvider.getDeleteImpact(u.id!);
    } on Exception catch (_) {
      // Still allow delete with a generic warning if preview fails.
    }

    if (!mounted) return;

    final name = '${u.firstName ?? ''} ${u.lastName ?? ''}'.trim();
    final reservationCount = impact?['reservationCount'] as int? ?? 0;
    final reviewCount = impact?['reviewCount'] as int? ?? 0;

    final warning = StringBuffer();
    warning.writeln(
      'Delete ${name.isEmpty ? 'this user' : name}?',
    );
    warning.writeln();
    if (reservationCount > 0 || reviewCount > 0) {
      warning.writeln(
        'Warning: this will permanently delete all related data, including:',
      );
      if (reservationCount > 0) {
        warning.writeln(
          '• $reservationCount reservation(s) and their reserved seats',
        );
      }
      if (reviewCount > 0) {
        warning.writeln('• $reviewCount review(s)');
      }
      warning.writeln('• notifications and login sessions for this account');
    } else {
      warning.writeln(
        'This account has no bookings. The user profile will still be removed permanently.',
      );
    }

    final ok = await confirmDelete(context, warning.toString().trim());
    if (ok != true || !mounted) return;
    try {
      await _userProvider.remove(u.id!);
      showAppSnackBar(context, 'User and related data deleted');
      _load();
    } on ApiClientException catch (e) {
      if (mounted) showAppSnackBar(context, e.message, isError: true);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showEmailDialog(User u) async {
    final subjectCtrl = TextEditingController();
    final bodyCtrl = TextEditingController();
    await showDialog(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.card,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: AppColors.cardBorder),
        ),
        title: Text('Email ${u.firstName}', style: const TextStyle(color: AppColors.textPrimary)),
        content: SizedBox(
          width: 420,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(controller: subjectCtrl, decoration: const InputDecoration(labelText: 'Subject')),
              const SizedBox(height: 12),
              TextField(
                controller: bodyCtrl,
                maxLines: 4,
                decoration: const InputDecoration(labelText: 'Message'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () async {
              try {
                await _userProvider.sendEmail(u.id!, subjectCtrl.text, bodyCtrl.text);
                if (context.mounted) {
                  Navigator.pop(context);
                  showAppSnackBar(this.context, 'Email sent');
                  context.read<NotificationProvider>().refresh();
                }
              } on ApiClientException catch (e) {
                if (context.mounted) showAppSnackBar(context, e.message, isError: true);
              } on Exception catch (e) {
                if (context.mounted) alertBox(context, 'Error', e.toString());
              }
            },
            child: const Text('Send'),
          ),
        ],
      ),
    );
  }

  Future<void> _showUserDialog({User? user}) async {
    User? fullUser = user;
    if (user?.id != null) {
      try {
        fullUser = await _userProvider.getById(user!.id!);
      } on Exception catch (e) {
        if (mounted) alertBox(context, 'Error', 'Could not load user: $e');
        return;
      }
    }

    if (!mounted) return;

    final firstCtrl = TextEditingController(text: fullUser?.firstName ?? '');
    final lastCtrl = TextEditingController(text: fullUser?.lastName ?? '');
    final emailCtrl = TextEditingController(text: fullUser?.email ?? '');
    final usernameCtrl = TextEditingController(text: fullUser?.username ?? '');
    final phoneCtrl = TextEditingController(text: fullUser?.phoneNumber ?? '');
    final passwordCtrl = TextEditingController();
    String selectedRole = (fullUser?.role?.isNotEmpty == true) ? fullUser!.role! : 'Customer';
    bool isActive = fullUser?.isActive ?? true;
    String? profileImageBase64 = fullUser?.profileImageBase64;
    bool submitting = false;
    final formKey = GlobalKey<FormState>();

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: user == null ? 'Add New User' : 'Edit User',
          submitLabel: user == null ? 'Add User' : 'Save',
          isSubmitting: submitting,
          maxWidth: 560,
          onSubmit: () async {
            if (!(formKey.currentState?.validate() ?? false)) return;

            setDialogState(() => submitting = true);
            try {
              if (user == null) {
                await _userProvider.insert({
                  'firstName': firstCtrl.text.trim(),
                  'lastName': lastCtrl.text.trim(),
                  'email': emailCtrl.text.trim(),
                  'username': usernameCtrl.text.trim(),
                  'password': passwordCtrl.text,
                  'phoneNumber': phoneCtrl.text.trim(),
                  'role': selectedRole,
                  'isActive': isActive,
                  'profileImageBase64': profileImageBase64,
                });
              } else {
                await _userProvider.update(user.id!, {
                  'firstName': firstCtrl.text.trim(),
                  'lastName': lastCtrl.text.trim(),
                  'email': emailCtrl.text.trim(),
                  'username': usernameCtrl.text.trim(),
                  'phoneNumber': phoneCtrl.text.trim(),
                  'role': selectedRole,
                  'isActive': isActive,
                  'profileImageBase64': profileImageBase64,
                });
              }
              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, user == null ? 'User added' : 'User updated');
                _load();
              }
            } on ApiClientException catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) showAppSnackBar(context, e.message, isError: true);
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) alertBox(context, 'Error', e.toString());
            }
          },
          child: Form(
            key: formKey,
            child: Column(
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
                        radius: 36,
                        backgroundColor: AppColors.inputFill,
                        backgroundImage: profileImageBase64 != null &&
                                profileImageBase64!.isNotEmpty
                            ? MemoryImage(base64Decode(profileImageBase64!))
                            : null,
                        child: profileImageBase64 == null || profileImageBase64!.isEmpty
                            ? const Icon(Icons.person, size: 32, color: AppColors.textSecondary)
                            : null,
                      ),
                    ),
                    const SizedBox(height: 6),
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
                      child: const Text('Upload profile photo'),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              Row(children: [
                Expanded(
                  child: TextFormField(
                    controller: firstCtrl,
                    decoration: const InputDecoration(labelText: 'First Name'),
                    validator: (v) => FieldValidators.required(v, field: 'First name'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: lastCtrl,
                    decoration: const InputDecoration(labelText: 'Last Name'),
                    validator: (v) => FieldValidators.required(v, field: 'Last name'),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              TextFormField(
                controller: emailCtrl,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: FieldValidators.email,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: usernameCtrl,
                decoration: const InputDecoration(labelText: 'Username'),
                validator: (v) => FieldValidators.required(v, field: 'Username'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: phoneCtrl,
                decoration: const InputDecoration(labelText: 'Phone Number'),
                validator: (v) => FieldValidators.phone(v),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: selectedRole,
                dropdownColor: AppColors.card,
                decoration: const InputDecoration(labelText: 'Role'),
                items: userRoles
                    .map((r) => DropdownMenuItem(value: r, child: Text(r)))
                    .toList(),
                onChanged: (v) => setDialogState(() => selectedRole = v ?? 'Customer'),
              ),
              const SizedBox(height: 12),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Active account'),
                value: isActive,
                onChanged: (v) => setDialogState(() => isActive = v),
              ),
              if (user == null) ...[
                const SizedBox(height: 12),
                TextFormField(
                  controller: passwordCtrl,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Password'),
                  validator: (v) => FieldValidators.minLength(v, 6, field: 'Password'),
                ),
              ],
            ],
            ),
          ),
        ),
      ),
    );
  }

  String _initials(String? first, String? last) {
    final f = (first?.isNotEmpty == true) ? first![0].toUpperCase() : '';
    final l = (last?.isNotEmpty == true) ? last![0].toUpperCase() : '';
    return '$f$l';
  }
}
