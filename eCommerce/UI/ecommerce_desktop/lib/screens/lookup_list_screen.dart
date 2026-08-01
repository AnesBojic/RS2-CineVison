import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/field_validators.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

/// The one field that differs between the reference tables.
enum LookupExtraField {
  none,

  /// Hall statuses: whether projections may be scheduled in halls with this status.
  allowsScreenings,

  /// Age ratings: the minimum viewer age.
  minimumAge,

  /// Languages: the short ISO-style code.
  code,
}

/// CRUD screen shared by every reference table. [P] is the provider that talks to
/// the matching API resource, e.g. `LookupListScreen<ScreenTypeProvider>`.
class LookupListScreen<P extends BaseProvider<LookupItem>> extends StatefulWidget {
  const LookupListScreen({
    super.key,
    required this.title,
    required this.itemNoun,
    this.extraField = LookupExtraField.none,
  });

  /// Plural heading, e.g. "Screen Types".
  final String title;

  /// Lower-case singular used in messages, e.g. "screen type".
  final String itemNoun;

  final LookupExtraField extraField;

  @override
  State<LookupListScreen<P>> createState() => _LookupListScreenState<P>();
}

class _LookupListScreenState<P extends BaseProvider<LookupItem>>
    extends State<LookupListScreen<P>> {
  late P _provider;
  List<LookupItem> _items = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<P>();
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
      final filter = <String, dynamic>{
        'page': 1,
        'pageSize': 100,
        'includeTotalCount': true,
      };
      final search = _searchController.text.trim();
      if (search.isNotEmpty) filter['name'] = search;

      final data = await _provider.get(filter: filter);
      if (!mounted) return;
      setState(() {
        _items = data.items ?? [];
        _loading = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      showAppSnackBar(context, e.toString(), isError: true);
    }
  }

  Future<void> _delete(LookupItem item) async {
    if (item.id == null) return;

    final ok = await confirmDelete(
      context,
      'Remove ${widget.itemNoun} "${item.name}"?',
    );
    if (ok != true) return;

    try {
      await _provider.remove(item.id!);
      if (!mounted) return;
      showAppSnackBar(context, '${_capitalized(widget.itemNoun)} deleted');
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot delete', e.message);
    } on Exception catch (e) {
      if (mounted) showAppSnackBar(context, e.toString(), isError: true);
    }
  }

  Future<void> _showEditor({LookupItem? existing}) async {
    final formKey = GlobalKey<FormState>();
    final nameCtrl = TextEditingController(text: existing?.name ?? '');
    final descCtrl = TextEditingController(text: existing?.description ?? '');
    final codeCtrl = TextEditingController(text: existing?.code ?? '');
    final minAgeCtrl = TextEditingController(
      text: existing?.minimumAge?.toString() ?? '',
    );
    var isActive = existing?.isActive ?? true;
    var allowsScreenings = existing?.allowsScreenings ?? false;
    var submitting = false;

    final saved = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => FormDialogShell(
          title: existing == null
              ? 'Add ${_capitalized(widget.itemNoun)}'
              : 'Edit ${_capitalized(widget.itemNoun)}',
          submitLabel: existing == null ? 'Add' : 'Save',
          isSubmitting: submitting,
          maxWidth: 480,
          onSubmit: () async {
            if (!(formKey.currentState?.validate() ?? false)) return;
            setLocal(() => submitting = true);
            try {
              final payload = LookupItem(
                name: nameCtrl.text.trim(),
                description: descCtrl.text.trim(),
                isActive: isActive,
                allowsScreenings: widget.extraField == LookupExtraField.allowsScreenings
                    ? allowsScreenings
                    : null,
                minimumAge: widget.extraField == LookupExtraField.minimumAge
                    ? int.tryParse(minAgeCtrl.text.trim())
                    : null,
                code: widget.extraField == LookupExtraField.code &&
                        codeCtrl.text.trim().isNotEmpty
                    ? codeCtrl.text.trim()
                    : null,
              );

              if (existing?.id == null) {
                await _provider.insert(payload.toJson());
              } else {
                await _provider.update(existing!.id!, payload.toJson());
              }
              if (ctx.mounted) Navigator.pop(ctx, true);
            } on ApiClientException catch (e) {
              setLocal(() => submitting = false);
              if (ctx.mounted) showAppSnackBar(ctx, e.message, isError: true);
            } on Exception catch (e) {
              setLocal(() => submitting = false);
              if (ctx.mounted) showAppSnackBar(ctx, e.toString(), isError: true);
            }
          },
          child: Form(
            key: formKey,
            child: Column(
              children: [
                TextFormField(
                  controller: nameCtrl,
                  decoration: const InputDecoration(labelText: 'Name'),
                  validator: (v) => FieldValidators.required(v, field: 'Name'),
                ),
                const SizedBox(height: 12),
                if (widget.extraField == LookupExtraField.code) ...[
                  TextFormField(
                    controller: codeCtrl,
                    maxLength: 10,
                    decoration: const InputDecoration(
                      labelText: 'Code',
                      hintText: 'e.g. en',
                      counterText: '',
                    ),
                  ),
                  const SizedBox(height: 12),
                ],
                if (widget.extraField == LookupExtraField.minimumAge) ...[
                  TextFormField(
                    controller: minAgeCtrl,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(
                      labelText: 'Minimum age',
                      hintText: 'Leave empty if the rating has no age limit',
                    ),
                    validator: (v) {
                      final text = (v ?? '').trim();
                      if (text.isEmpty) return null;
                      final parsed = int.tryParse(text);
                      if (parsed == null) return 'Enter a whole number.';
                      if (parsed < 0 || parsed > 21) {
                        return 'Minimum age must be between 0 and 21.';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 12),
                ],
                TextFormField(
                  controller: descCtrl,
                  maxLines: 3,
                  decoration: const InputDecoration(labelText: 'Description'),
                ),
                if (widget.extraField == LookupExtraField.allowsScreenings)
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Projections can be scheduled'),
                    subtitle: const Text(
                      'Halls with this status appear when scheduling a projection.',
                    ),
                    value: allowsScreenings,
                    onChanged: (v) => setLocal(() => allowsScreenings = v),
                  ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Active'),
                  value: isActive,
                  onChanged: (v) => setLocal(() => isActive = v),
                ),
              ],
            ),
          ),
        ),
      ),
    );

    nameCtrl.dispose();
    descCtrl.dispose();
    codeCtrl.dispose();
    minAgeCtrl.dispose();

    if (saved == true && mounted) {
      showAppSnackBar(
        context,
        existing == null
            ? '${_capitalized(widget.itemNoun)} added'
            : '${_capitalized(widget.itemNoun)} updated',
      );
      await _load();
    }
  }

  static String _capitalized(String value) =>
      value.isEmpty ? value : value[0].toUpperCase() + value.substring(1);

  String _subtitleFor(LookupItem item) {
    final parts = <String>[
      item.isActive == true ? 'Active' : 'Inactive',
      if (widget.extraField == LookupExtraField.code && (item.code ?? '').isNotEmpty)
        'Code: ${item.code}',
      if (widget.extraField == LookupExtraField.minimumAge && item.minimumAge != null)
        'Minimum age: ${item.minimumAge}',
      if (widget.extraField == LookupExtraField.allowsScreenings)
        item.allowsScreenings == true
            ? 'Projections allowed'
            : 'Projections not allowed',
      if ((item.description ?? '').isNotEmpty) item.description!,
    ];
    return parts.join(' · ');
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage ${widget.title}',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search ${widget.title.toLowerCase()}',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 12),
          PrimaryButton(label: 'Refresh', onPressed: _load),
          const SizedBox(width: 8),
          PrimaryButton(
            label: 'Add ${_capitalized(widget.itemNoun)}',
            onPressed: () => _showEditor(),
          ),
        ],
      ),
      child: _items.isEmpty
          ? Center(child: Text('No ${widget.title.toLowerCase()} yet.'))
          : ListView.separated(
              itemCount: _items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) {
                final item = _items[index];
                return Card(
                  color: AppColors.card,
                  child: ListTile(
                    title: Row(
                      children: [
                        Text(
                          item.name ?? '',
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(width: 10),
                        if (item.inUseCount > 0)
                          StatusBadge(
                            label: 'In use: ${item.inUseCount}',
                            color: AppColors.textSecondary,
                          ),
                      ],
                    ),
                    subtitle: Text(
                      _subtitleFor(item),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        ActionIconButton(
                          icon: Icons.edit_outlined,
                          color: AppColors.blue,
                          tooltip: 'Edit',
                          onPressed: () => _showEditor(existing: item),
                        ),
                        const SizedBox(width: 8),
                        ActionIconButton(
                          icon: Icons.delete_outline,
                          color: AppColors.primary,
                          enabled: item.canDelete,
                          tooltip: item.canDelete
                              ? 'Delete'
                              : item.deleteBlockedReason ??
                                  'This ${widget.itemNoun} is still in use.',
                          onPressed: () => _delete(item),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
    );
  }
}
