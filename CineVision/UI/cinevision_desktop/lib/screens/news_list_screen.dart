import 'dart:convert';

import 'package:cinevision_desktop/core/theme/app_theme.dart';
import 'package:cinevision_desktop/core/utils/utc_datetime.dart';
import 'package:cinevision_desktop/core/widgets/cinevision_widgets.dart';
import 'package:cinevision_desktop/models/news.dart';
import 'package:cinevision_desktop/providers/news_provider.dart';
import 'package:cinevision_desktop/utils/field_validators.dart';
import 'package:cinevision_desktop/utils/image_utils.dart';
import 'package:cinevision_desktop/utils/utils_widgets.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class NewsListScreen extends StatefulWidget {
  const NewsListScreen({super.key});

  @override
  State<NewsListScreen> createState() => _NewsListScreenState();
}

class _NewsListScreenState extends State<NewsListScreen> {
  late NewsProvider _provider;
  List<NewsItem> _items = [];
  bool _loading = true;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<NewsProvider>();
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
        'pageSize': 50,
        'includeTotalCount': true,
      };
      if (_searchController.text.trim().isNotEmpty) {
        filter['title'] = _searchController.text.trim();
      }

      final data = await _provider.get(filter: filter);
      if (!mounted) return;
      setState(() {
        _items = data.items ?? [];
        _loading = false;
      });
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  Future<void> _delete(NewsItem item) async {
    final ok = await confirmDelete(context, 'Remove "${item.title}" permanently?');
    if (ok != true || item.id == null) return;

    try {
      await _provider.remove(item.id!);
      if (!mounted) return;
      setState(() => _items = _items.where((x) => x.id != item.id).toList());
      await _load();
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showEditor({NewsItem? existing}) async {
    final formKey = GlobalKey<FormState>();
    final titleCtrl = TextEditingController(text: existing?.title ?? '');
    final contentCtrl = TextEditingController(text: existing?.content ?? '');
    String? imageBase64 = existing?.imageBase64;
    var submitting = false;

    final saved = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setLocal) {
            return FormDialogShell(
              title: existing == null ? 'New announcement' : 'Edit announcement',
              submitLabel: existing == null ? 'Publish' : 'Save',
              isSubmitting: submitting,
              maxWidth: 520,
              onSubmit: () async {
                if (!(formKey.currentState?.validate() ?? false)) return;
                if (imageBase64 == null || imageBase64!.isEmpty) {
                  showAppSnackBar(ctx, 'News image is required.', isError: true);
                  return;
                }
                setLocal(() => submitting = true);
                try {
                  final payload = NewsItem(
                    title: titleCtrl.text.trim(),
                    content: contentCtrl.text.trim(),
                    imageBase64: imageBase64,
                    publishedAt: existing?.publishedAt ?? UtcDateTime.now(),
                  );
                  if (existing?.id == null) {
                    await _provider.insert(payload.toInsertJson());
                  } else {
                    await _provider.update(existing!.id!, payload.toUpdateJson());
                  }
                  if (ctx.mounted) Navigator.pop(ctx, true);
                } on Exception catch (e) {
                  setLocal(() => submitting = false);
                  if (ctx.mounted) showAppSnackBar(ctx, e.toString(), isError: true);
                }
              },
              child: Form(
                key: formKey,
                child: Column(
                  children: [
                    PosterUploadBox(
                      base64: imageBase64,
                      hint: 'Click to upload news image',
                      onPick: () async {
                        final result = await FilePicker.pickFiles(
                          type: FileType.image,
                          withData: true,
                        );
                        if (result != null && result.files.single.bytes != null) {
                          final compressed =
                              await preparePosterBase64(result.files.single.bytes!);
                          setLocal(() => imageBase64 = compressed);
                        }
                      },
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: titleCtrl,
                      decoration: const InputDecoration(labelText: 'Title'),
                      validator: (v) => FieldValidators.required(v, field: 'Title'),
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: contentCtrl,
                      maxLines: 6,
                      decoration: const InputDecoration(labelText: 'Content'),
                      validator: (v) => FieldValidators.required(v, field: 'Content'),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );

    titleCtrl.dispose();
    contentCtrl.dispose();

    if (saved == true) await _load();
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'News & Announcements',
      isLoading: _loading,
      toolbar: Row(
        children: [
          SearchField(
            controller: _searchController,
            hint: 'Search by title',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 12),
          PrimaryButton(label: 'Refresh', onPressed: _load),
          const SizedBox(width: 8),
          PrimaryButton(label: 'Add news', onPressed: () => _showEditor()),
        ],
      ),
      child: _items.isEmpty
          ? const Center(child: Text('No announcements yet.'))
          : ListView.separated(
              itemCount: _items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) {
                final item = _items[index];
                final published = item.publishedAt?.toLocal();
                final hasImage =
                    item.imageBase64 != null && item.imageBase64!.isNotEmpty;
                return Card(
                  color: AppColors.card,
                  child: ListTile(
                    leading: hasImage
                        ? ClipRRect(
                            borderRadius: BorderRadius.circular(8),
                            child: Image.memory(
                              base64Decode(item.imageBase64!),
                              width: 56,
                              height: 56,
                              fit: BoxFit.cover,
                            ),
                          )
                        : const CircleAvatar(child: Icon(Icons.article_outlined)),
                    title: Text(item.title ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text(
                      [
                        if (published != null) 'Published ${published.toString().split('.').first}',
                        item.content ?? '',
                      ].join(' · '),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        IconButton(
                          tooltip: 'Edit',
                          onPressed: () => _showEditor(existing: item),
                          icon: const Icon(Icons.edit_outlined),
                        ),
                        IconButton(
                          tooltip: 'Delete',
                          onPressed: () => _delete(item),
                          icon: const Icon(Icons.delete_outline),
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
