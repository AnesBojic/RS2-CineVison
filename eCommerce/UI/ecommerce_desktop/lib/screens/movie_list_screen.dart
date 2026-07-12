import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/genre.dart';
import 'package:ecommerce_desktop/models/movie.dart';
import 'package:ecommerce_desktop/providers/genre_provider.dart';
import 'package:ecommerce_desktop/providers/movie_provider.dart';
import 'package:ecommerce_desktop/utils/image_utils.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/utils_widgets.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class MovieListScreen extends StatefulWidget {
  const MovieListScreen({super.key, this.editId, this.onEditConsumed});

  final int? editId;
  final VoidCallback? onEditConsumed;

  @override
  State<MovieListScreen> createState() => _MovieListScreenState();
}

class _MovieListScreenState extends State<MovieListScreen> {
  late MovieProvider _movieProvider;
  List<Movie> _movies = [];
  List<Genre> _genres = [];
  bool _loading = true;
  final _searchController = TextEditingController();
  String? _genreFilter;
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    _movieProvider = context.read<MovieProvider>();
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
      final genres = await context.read<GenreProvider>().get(filter: {'pageSize': 100});
      final filter = <String, dynamic>{'includeGenre': true, 'pageSize': 50};
      if (_searchController.text.isNotEmpty) filter['title'] = _searchController.text;
      if (_genreFilter != null) filter['genreId'] = int.tryParse(_genreFilter!);
      if (_statusFilter != null) {
        filter['movieState'] = MovieState.filterValueForLabel(_statusFilter!);
      }

      final data = await _movieProvider.get(filter: filter, includePoster: true);
      if (!mounted) return;
      setState(() {
        _genres = genres.items ?? [];
        _movies = data.items ?? [];
        _loading = false;
      });
      _maybeOpenEdit();
    } on Exception catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        alertBox(context, 'Error', e.toString());
      }
    }
  }

  void _maybeOpenEdit() {
    final id = widget.editId;
    if (id == null) return;
    Movie? movie;
    for (final m in _movies) {
      if (m.id == id) {
        movie = m;
        break;
      }
    }
    widget.onEditConsumed?.call();
    if (movie != null && mounted) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _showMovieDialog(movie: movie);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return ManagePageLayout(
      title: 'Manage Movies',
      isLoading: _loading,
      toolbar: Row(
        children: [
          FilterDropdown(
            hint: 'All Genres',
            value: _genreFilter,
            items: [
              const DropdownMenuItem(value: null, child: Text('All Genres')),
              ..._genres.map((g) => DropdownMenuItem(value: '${g.id}', child: Text(g.name ?? ''))),
            ],
            onChanged: (v) {
              setState(() => _genreFilter = v);
              _load();
            },
          ),
          const SizedBox(width: 10),
          FilterDropdown(
            hint: 'All Status',
            value: _statusFilter,
            items: const [
              DropdownMenuItem(value: null, child: Text('All Status')),
              DropdownMenuItem(value: 'Active', child: Text('Active')),
              DropdownMenuItem(value: 'Draft', child: Text('Draft')),
            ],
            onChanged: (v) {
              setState(() => _statusFilter = v);
              _load();
            },
          ),
          const SizedBox(width: 10),
          SearchField(
            controller: _searchController,
            hint: 'Search movies...',
            onSubmitted: (_) => _load(),
          ),
          const SizedBox(width: 10),
          PrimaryButton(label: 'Add Movie', onPressed: () => _showMovieDialog()),
        ],
      ),
      child: DataCard(
        emptyMessage: _movies.isEmpty ? 'No movies found' : null,
        child: StyledDataTable(
          columns: const [
            DataColumn(label: Text('Movie Title')),
            DataColumn(label: Text('Genre')),
            DataColumn(label: Text('Rating')),
            DataColumn(label: Text('Duration')),
            DataColumn(label: Text('Release Date')),
            DataColumn(label: Text('Status')),
            DataColumn(label: Text('Actions')),
          ],
          rows: _movies.map(_buildRow).toList(),
        ),
      ),
    );
  }

  DataRow _buildRow(Movie m) {
    final isActive = m.isActiveState;
    return DataRow(cells: [
      DataCell(Row(children: [
        posterThumbnail(m.posterImageBase64),
        const SizedBox(width: 12),
        Text(m.title ?? '—', style: const TextStyle(fontWeight: FontWeight.w500)),
      ])),
      DataCell(Text(m.genre?.name ?? '—')),
      DataCell(Text(m.ageRating ?? '—')),
      DataCell(Text('${m.durationMinutes ?? 0} min')),
      DataCell(Text(formatDate(m.releaseDate))),
      DataCell(StatusBadge(
        label: m.displayState,
        color: isActive ? AppColors.green : AppColors.orange,
        filled: true,
      )),
      DataCell(Row(children: [
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          onPressed: () => _showMovieDialog(movie: m),
        ),
        const SizedBox(width: 8),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          onPressed: () => _delete(m),
        ),
      ])),
    ]);
  }

  Future<void> _delete(Movie m) async {
    final ok = await confirmDelete(context, 'Delete "${m.title}"?');
    if (ok != true || !mounted) return;
    try {
      await _movieProvider.remove(m.id!);
      showAppSnackBar(context, 'Movie deleted');
      _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot delete', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showMovieDialog({Movie? movie}) async {
    Movie? fullMovie = movie;
    if (movie?.id != null) {
      try {
        fullMovie = await _movieProvider.getWithPoster(movie!.id!);
      } on Exception catch (e) {
        if (mounted) alertBox(context, 'Error', 'Could not load movie details: $e');
        return;
      }
    }

    final titleCtrl = TextEditingController(text: fullMovie?.title ?? '');
    final durationCtrl = TextEditingController(text: '${fullMovie?.durationMinutes ?? ''}');
    final ratingCtrl = TextEditingController(text: fullMovie?.ageRating ?? '');
    int? genreId = fullMovie?.genreId;
    DateTime? releaseDate = fullMovie?.releaseDate;
    String statusSelection = fullMovie?.isActiveState == true ? 'Active' : 'Draft';
    String? posterBase64 = fullMovie?.posterImageBase64;
    String? originalPoster = fullMovie?.posterImageBase64;
    bool submitting = false;

    if (!mounted) return;

    await showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => FormDialogShell(
          title: movie == null ? 'Add New Movie' : 'Edit Movie',
          submitLabel: movie == null ? 'Add Movie' : 'Save',
          isSubmitting: submitting,
          maxWidth: 620,
          onSubmit: () async {
            if (titleCtrl.text.trim().isEmpty) {
              alertBox(context, 'Validation', 'Movie title is required');
              return;
            }
            setDialogState(() => submitting = true);
            final payload = Movie(
              title: titleCtrl.text.trim(),
              description: '',
              durationMinutes: int.tryParse(durationCtrl.text) ?? 0,
              genreId: genreId,
              releaseDate: releaseDate,
              ageRating: ratingCtrl.text.trim(),
            );
            try {
              Movie saved;
              if (movie == null) {
                saved = await _movieProvider.insert(payload.toInsertJson());
                if (statusSelection == 'Active') {
                  saved = await _movieProvider.activate(saved.id!);
                }
              } else {
                saved = await _movieProvider.update(movie.id!, payload.toUpdateJson());
                final wantsActive = statusSelection == 'Active';
                if (wantsActive && !movie.isActiveState) {
                  saved = await _movieProvider.activate(movie.id!);
                } else if (!wantsActive && movie.isActiveState) {
                  saved = await _movieProvider.deactivate(movie.id!);
                }
              }

              if (posterBase64 != null &&
                  posterBase64!.isNotEmpty &&
                  posterBase64 != originalPoster) {
                await _movieProvider.uploadPoster(saved.id!, posterBase64!);
              }

              if (context.mounted) {
                Navigator.pop(context);
                showAppSnackBar(this.context, movie == null ? 'Movie added' : 'Movie updated');
                _load();
              }
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) alertBox(context, 'Error', e.toString());
            }
          },
          child: Column(
            children: [
              PosterUploadBox(
                base64: posterBase64,
                onPick: () async {
                  final result = await FilePicker.pickFiles(type: FileType.image, withData: true);
                  if (result != null && result.files.single.bytes != null) {
                    final compressed = await preparePosterBase64(result.files.single.bytes!);
                    setDialogState(() => posterBase64 = compressed);
                  }
                },
              ),
              const SizedBox(height: 16),
              Row(children: [
                Expanded(
                  child: TextField(
                    controller: titleCtrl,
                    decoration: const InputDecoration(labelText: 'Movie Title', hintText: 'Enter movie title'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: genreId,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Genre'),
                    items: _genres
                        .map((g) => DropdownMenuItem(value: g.id, child: Text(g.name ?? '')))
                        .toList(),
                    onChanged: (v) => setDialogState(() => genreId = v),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextField(
                    controller: ratingCtrl,
                    decoration: const InputDecoration(labelText: 'Rating', hintText: 'e.g., PG-13'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    controller: durationCtrl,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Duration', hintText: 'e.g., 192 min'),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: context,
                        initialDate: releaseDate ?? DateTime.now(),
                        firstDate: DateTime(1900),
                        lastDate: DateTime(2100),
                      );
                      if (picked != null) setDialogState(() => releaseDate = picked);
                    },
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Release Date'),
                      child: Text(formatDate(releaseDate)),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: statusSelection,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Status'),
                    items: const [
                      DropdownMenuItem(value: 'Active', child: Text('Active')),
                      DropdownMenuItem(value: 'Draft', child: Text('Draft')),
                    ],
                    onChanged: (v) => setDialogState(() => statusSelection = v ?? 'Draft'),
                  ),
                ),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}
