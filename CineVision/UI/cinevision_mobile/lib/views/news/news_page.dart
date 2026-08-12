import 'dart:convert';

import 'package:cinevision_mobile/core/components/app_back_button.dart';
import 'package:cinevision_mobile/core/constants/app_defaults.dart';
import 'package:cinevision_mobile/models/news.dart';
import 'package:cinevision_mobile/providers/news_provider.dart';
import 'package:cinevision_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class NewsPage extends StatefulWidget {
  const NewsPage({super.key});

  @override
  State<NewsPage> createState() => _NewsPageState();
}

class _NewsPageState extends State<NewsPage> {
  List<NewsItem> _items = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final result = await context.read<NewsProvider>().get(filter: {
        'page': 1,
        'pageSize': 30,
      });
      if (!mounted) return;
      setState(() {
        _items = (result.items ?? []).toList()
          ..sort((a, b) {
            final ad = a.publishedAt ?? DateTime.fromMillisecondsSinceEpoch(0);
            final bd = b.publishedAt ?? DateTime.fromMillisecondsSinceEpoch(0);
            return bd.compareTo(ad);
          });
        _loading = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _loading = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: const AppBackButton(),
        title: const Text('Cinema News'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _items.isEmpty
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 120),
                        Center(child: Text('No news right now.')),
                      ],
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.all(AppDefaults.padding),
                      itemCount: _items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 12),
                      itemBuilder: (context, index) {
                        final item = _items[index];
                        final published = item.publishedAt?.toLocal();
                        final hasImage = item.imageBase64 != null &&
                            item.imageBase64!.isNotEmpty;
                        return Card(
                          clipBehavior: Clip.antiAlias,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              if (hasImage)
                                Image.memory(
                                  base64Decode(item.imageBase64!),
                                  height: 160,
                                  fit: BoxFit.cover,
                                ),
                              Padding(
                                padding: const EdgeInsets.all(16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      item.title ?? '',
                                      style: Theme.of(context)
                                          .textTheme
                                          .titleMedium
                                          ?.copyWith(fontWeight: FontWeight.bold),
                                    ),
                                    if (published != null) ...[
                                      const SizedBox(height: 4),
                                      Text(
                                        '${published.year}-${published.month.toString().padLeft(2, '0')}-${published.day.toString().padLeft(2, '0')}',
                                        style:
                                            Theme.of(context).textTheme.bodySmall,
                                      ),
                                    ],
                                    const SizedBox(height: 8),
                                    Text(item.content ?? ''),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}
