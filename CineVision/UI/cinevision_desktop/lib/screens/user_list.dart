import 'dart:convert';

import 'package:cinevision_desktop/core/enums/api_enums.dart';
import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/lookup_item.dart';
import 'package:cinevision_desktop/models/user.dart';
import 'package:cinevision_desktop/providers/notification_provider.dart';
import 'package:cinevision_desktop/providers/role_provider.dart';
import 'package:cinevision_desktop/providers/user_provider.dart';
import 'package:cinevision_desktop/utils/api_client_exception.dart';
import 'package:cinevision_desktop/utils/field_validators.dart';
import 'package:cinevision_desktop/utils/image_utils.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
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
  late RoleProvider _roleProvider;
  List<User> _users = [];
  List<LookupItem> _roles = [];
  bool _loading = true;
  final _searchController = TextEditingController();
  String? _roleFilter;
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    _userProvider = context.read<UserProvider>();
    _roleProvider = context.read<RoleProvider>();
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
      if (_searchController.text.trim().isNotEmpty) {
        filter['name'] = _searchController.text.trim();
      }
      if (_roleFilter != null) {
        filter['role'] = _roleFilter;
      }
      if (_statusFilter != null) {
        filter['isActive'] = _statusFilter == 'true';
      }
      final data = await _userProvider.get(filter: filter);
      // Roles are reference data too, so the picker offers whatever the database holds.
      final roles = await _roleProvider.get(
        filter: {'pageSize': 100},
      );
      if (!mounted) return;
      setState(() {
        _users = data.items ?? [];
        _roles = roles.items ?? [];
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
          FilterDropdown(
            hint: 'All Roles',
            value: _roleFilter,
            items: [
              const DropdownMenuItem(value: null, child: Text('All Roles')),
              ..._roles.map(
                (r) => DropdownMenuItem(
                  value: r.name,
                  child: Text(r.name ?? ''),
                ),
              ),
            ],
            onChanged: (v) {
              setState(() => _roleFilter = v);
              _load();
            },
          ),
          const SizedBox(width: 10),
          FilterDropdown(
            hint: 'All Status',
            value: _statusFilter,
            items: const [
              DropdownMenuItem(value: null, child: Text('All Status')),
              DropdownMenuItem(value: 'true', child: Text('Active')),
              DropdownMenuItem(value: 'false', child: Text('Inactive')),
            ],
            onChanged: (v) {
              setState(() => _statusFilter = v);
              _load();
            },
          ),
          const SizedBox(width: 10),
          SearchField(
            controller: _searchController,
            hint: 'Search name, email, username...',
            width: 280,
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 10),
          PrimaryButton(
            label: 'Add User',
            onPressed: _missingRolesReason == null ? () => _showUserDialog() : null,
            tooltip: _missingRolesReason,
          ),
        ],
      ),
      child: DataCard(
        emptyMessage: _users.isEmpty ? 'No users found' : null,
        child: StyledDataTable(
          columns: const [
            DataColumn(label: Text('Name')),
            DataColumn(label: Text('Email')),
            DataColumn(label: Text('Role')),
            DataColumn(label: Text('Status')),
            DataColumn(label: Text('Date Created')),
            DataColumn(label: Text('Date Modified')),
            actionsDataColumn,
          ],
          rows: _users.map(_buildRow).toList(),
        ),
      ),
    );
  }

  DataRow _buildRow(User u) {
    final fullName = '${u.firstName ?? ''} ${u.lastName ?? ''}'.trim();
    final active = u.isActive ?? true;
    return DataRow(cells: [
      DataCell(Row(children: [
        CircleAvatar(
          radius: 16,
          backgroundColor: AppColors.inputFill,
          child: Text(
            _initials(u.firstName, u.lastName),
            style: TextStyle(
              fontSize: 11,
              color: AppColors.textSecondary,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        const SizedBox(width: 12),
        Text(
          fullName.isEmpty ? '—' : fullName,
          style: TextStyle(
            fontWeight: FontWeight.w500,
            color: active ? null : AppColors.textSecondary,
          ),
        ),
      ])),
      DataCell(Text(u.email ?? '—')),
      DataCell(RoleBadge(role: (u.role?.isNotEmpty == true) ? u.role! : 'Customer')),
      DataCell(StatusBadge(
        label: active ? 'Active' : 'Inactive',
        color: active ? AppColors.green : AppColors.orange,
        filled: true,
      )),
      DataCell(Text(formatDate(u.createdAt))),
      DataCell(Text(formatDate(u.updatedAt))),
      actionButtonsCell([
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          onPressed: () => _showUserDialog(user: u),
        ),
        ActionIconButton(
          icon: Icons.mail_outline,
          color: AppColors.green,
          onPressed: () => _showEmailDialog(u),
        ),
        ActionIconButton(
          icon: active ? Icons.person_off_outlined : Icons.person_outline,
          color: active ? AppColors.orange : AppColors.green,
          tooltip: active ? 'Deactivate account' : 'Activate account',
          onPressed: () => _toggleActive(u),
        ),
      ]),
    ]);
  }

  Future<void> _toggleActive(User u) async {
    if (u.id == null) return;

    final currentlyActive = u.isActive ?? true;
    final name = '${u.firstName ?? ''} ${u.lastName ?? ''}'.trim();
    final label = name.isEmpty ? 'this user' : name;
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.card,
        title: Text(currentlyActive ? 'Deactivate user?' : 'Activate user?'),
        content: Text(
          currentlyActive
              ? '$label will stay in the list but cannot sign in until reactivated.'
              : '$label will be able to sign in again.',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(currentlyActive ? 'Deactivate' : 'Activate'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;

    try {
      await _userProvider.setActive(u.id!, !currentlyActive);
      if (!mounted) return;
      await _load();
      if (!mounted) return;
      showAppSnackBar(
        context,
        currentlyActive ? 'User deactivated' : 'User activated',
      );
    } on ApiClientException catch (e) {
      if (!mounted) return;
      showAppSnackBar(context, e.message, isError: true);
    } on Exception catch (e) {
      if (!mounted) return;
      alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showEmailDialog(User u) async {
    final subjectCtrl = TextEditingController();
    final bodyCtrl = TextEditingController();
    final emailFormKey = GlobalKey<FormState>();
    bool submitting = false;

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: 'Email ${u.firstName}',
          submitLabel: 'Send',
          isSubmitting: submitting,
          maxWidth: 460,
          onSubmit: () async {
            if (!(emailFormKey.currentState?.validate() ?? false)) return;
            setDialogState(() => submitting = true);
            final notifications = this.context.read<NotificationProvider>();
            try {
              await _userProvider.sendEmail(u.id!, subjectCtrl.text, bodyCtrl.text);
              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, 'Email sent');
                notifications.refresh();
              }
            } on ApiClientException catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) showAppSnackBar(context, e.message, isError: true);
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) showAppSnackBar(context, e.toString(), isError: true);
            }
          },
          child: Form(
            key: emailFormKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: subjectCtrl,
                  decoration: const InputDecoration(labelText: 'Subject'),
                  validator: (v) => FieldValidators.required(v, field: 'Subject'),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: bodyCtrl,
                  maxLines: 4,
                  decoration: const InputDecoration(labelText: 'Message'),
                  validator: (v) => FieldValidators.required(v, field: 'Message'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  /// Null when roles have been loaded for the user form.
  String? get _missingRolesReason => _loading || _roles.isNotEmpty
      ? null
      : 'No roles are available. A user cannot be saved without one.';

  Future<void> _showUserDialog({User? user}) async {
    final blockedReason = _missingRolesReason;
    if (blockedReason != null) {
      showAppSnackBar(context, blockedReason, isError: true);
      return;
    }

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
    final roleNames = _roles
        .map((r) => r.name ?? '')
        .where((name) => name.isNotEmpty)
        .toList();
    final currentRole = fullUser?.role;
    String selectedRole = roleNames.contains(currentRole)
        ? currentRole!
        : roleNames.firstWhere(
            (name) => name == UserRoles.customer,
            orElse: () => roleNames.first,
          );
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
                items: _roles
                    .map((r) => DropdownMenuItem(
                          value: r.name,
                          child: Text(r.name ?? ''),
                        ))
                    .toList(),
                onChanged: (v) =>
                    setDialogState(() => selectedRole = v ?? selectedRole),
                validator: (v) =>
                    (v == null || v.isEmpty) ? 'Role is required' : null,
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
