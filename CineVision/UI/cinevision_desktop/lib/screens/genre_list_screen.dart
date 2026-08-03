import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/genre.dart';
import 'package:cinevision_desktop/providers/genre_provider.dart';
import 'package:cinevision_desktop/utils/field_validators.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class GenreListScreen extends StatefulWidget {
  const GenreListScreen({super.key});

  @override
  State<GenreListScreen> createState() => _GenreListScreenState();
}

class _GenreListScreenState extends State<GenreListScreen> {
  late GenreProvider _provider;
  List<Genre> _items = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<GenreProvider>();
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
      if (_searchController.text.trim().isNotEmpty) {
        filter['name'] = _searchController.text.trim();
      }
      final data = await _provider.get(filter: filter);
      if (!mounted) return;
      final items = data.items ?? [];
      setState(() {
        _items = items;
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        showAppSnackBar(context, e.toString(), isError: true);
      }
    }
  }

  Future<void> _delete(Genre item) async {
    final ok = await confirmDelete(context, 'Remove genre "${item.name}"?');
    if (ok != true || item.id == null) return;
    try {
      await _provider.remove(item.id!);
      if (!mounted) return;
      showAppSnackBar(context, 'Genre deleted');
      setState(() => _items = _items.where((x) => x.id != item.id).toList());
      await _load();
    } on Exception catch (e) {
      if (mounted) showAppSnackBar(context, e.toString(), isError: true);
    }
  }

  Future<void> _showEditor({Genre? existing}) async {
    final formKey = GlobalKey<FormState>();
    final nameCtrl = TextEditingController(text: existing?.name ?? '');
    final descCtrl = TextEditingController(text: existing?.description ?? '');
    var isActive = existing?.isActive ?? true;
    var submitting = false;

    final saved = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setLocal) {
            return FormDialogShell(
              title: existing == null ? 'Add Genre' : 'Edit Genre',
              submitLabel: existing == null ? 'Add Genre' : 'Save',
              isSubmitting: submitting,
              maxWidth: 480,
              onSubmit: () async {
                if (!(formKey.currentState?.validate() ?? false)) return;
                setLocal(() => submitting = true);
                try {
                  final payload = Genre(
                    name: nameCtrl.text.trim(),
                    description: descCtrl.text.trim(),
                    isActive: isActive,
                  );
                  if (existing?.id == null) {
                    await _provider.insert(payload.toInsertJson());
                  } else {
                    await _provider.update(existing!.id!, payload.toUpdateJson());
                  }
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } on Exception catch (e) {
                  setLocal(() => submitting = false);
                  if (ctx.mounted) {
                    showAppSnackBar(ctx, e.toString(), isError: true);
                  }
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
                    TextFormField(
                      controller: descCtrl,
                      maxLines: 3,
                      decoration: const InputDecoration(labelText: 'Description'),
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
            );
          },
        );
      },
    );

    nameCtrl.dispose();
    descCtrl.dispose();
    if (saved == true) {
      showAppSnackBar(context, existing == null ? 'Genre added' : 'Genre updated');
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage Genres',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search genres',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 12),
          PrimaryButton(label: 'Refresh', onPressed: _load),
          const SizedBox(width: 8),
          PrimaryButton(label: 'Add Genre', onPressed: () => _showEditor()),
        ],
      ),
      child: _items.isEmpty
          ? const Center(child: Text('No genres yet.'))
          : ListView.separated(
              itemCount: _items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) {
                final item = _items[index];
                return Card(
                  color: AppColors.card,
                  child: ListTile(
                    title: Text(
                      item.name ?? '',
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    subtitle: Text(
                      [
                        item.isActive == true ? 'Active' : 'Inactive',
                        if ((item.description ?? '').isNotEmpty) item.description!,
                      ].join(' · '),
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
                          tooltip: 'Delete',
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
