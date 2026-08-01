import 'package:ecommerce_desktop/core/theme/app_theme.dart';
import 'package:ecommerce_desktop/core/widgets/cinevision_widgets.dart';
import 'package:ecommerce_desktop/models/genre.dart';
import 'package:ecommerce_desktop/models/lookup_item.dart';
import 'package:ecommerce_desktop/models/movie.dart';
import 'package:ecommerce_desktop/providers/age_rating_provider.dart';
import 'package:ecommerce_desktop/providers/genre_provider.dart';
import 'package:ecommerce_desktop/providers/language_provider.dart';
import 'package:ecommerce_desktop/providers/movie_provider.dart';
import 'package:ecommerce_desktop/utils/field_validators.dart';
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
  late GenreProvider _genreProvider;
  late AgeRatingProvider _ageRatingProvider;
  late LanguageProvider _languageProvider;
  List<Movie> _movies = [];
  List<Genre> _genres = [];
  List<LookupItem> _ageRatings = [];
  List<LookupItem> _languages = [];
  bool _loading = true;
  final _searchController = TextEditingController();
  String? _genreFilter;
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    _movieProvider = context.read<MovieProvider>();
    _genreProvider = context.read<GenreProvider>();
    _ageRatingProvider = context.read<AgeRatingProvider>();
    _languageProvider = context.read<LanguageProvider>();
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
      const lookupFilter = {'pageSize': 100, 'isActive': true};
      final genres = await _genreProvider.get(filter: {'pageSize': 100});
      final ageRatings = await _ageRatingProvider.get(filter: lookupFilter);
      final languages = await _languageProvider.get(filter: lookupFilter);

      final filter = <String, dynamic>{'includeGenre': true, 'pageSize': 50};
      if (_searchController.text.isNotEmpty) filter['title'] = _searchController.text;
      if (_genreFilter != null) filter['genreId'] = int.tryParse(_genreFilter!);
      if (_statusFilter != null) {
        filter['movieState'] = _statusFilter;
      }

      final data = await _movieProvider.get(filter: filter, includePoster: true);
      if (!mounted) return;
      final movies = data.items ?? [];
      movies.sort((a, b) => (b.id ?? 0).compareTo(a.id ?? 0));
      setState(() {
        _genres = genres.items ?? [];
        _ageRatings = ageRatings.items ?? [];
        _languages = languages.items ?? [];
        _movies = movies;
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
            items: [
              const DropdownMenuItem(value: null, child: Text('All Status')),
              for (final state in MovieState.all)
                DropdownMenuItem(value: state, child: Text(state)),
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
          key: ValueKey(_movies.map((m) => '${m.id}-${m.displayState}').join('|')),
          columns: const [
            DataColumn(label: Text('Movie Title')),
            DataColumn(label: Text('Genre')),
            DataColumn(label: Text('Rating')),
            DataColumn(label: Text('Duration')),
            DataColumn(label: Text('Release Date')),
            DataColumn(label: Text('Status')),
            actionsDataColumn,
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
      actionButtonsCell([
        ActionIconButton(
          icon: Icons.edit_outlined,
          color: AppColors.blue,
          tooltip: 'Edit',
          onPressed: () => _showMovieDialog(movie: m),
        ),
        ActionIconButton(
          icon: Icons.delete_outline,
          color: AppColors.primary,
          tooltip: 'Delete',
          onPressed: () => _delete(m),
        ),
      ]),
    ]);
  }

  Future<void> _delete(Movie m) async {
    if (m.id == null) return;

    Map<String, dynamic>? impact;
    try {
      impact = await _movieProvider.getDeleteImpact(m.id!);
    } on Exception catch (_) {}

    if (!mounted) return;
    final ok = await confirmDelete(
      context,
      buildCascadeDeleteWarning(
        subjectLabel: '"${m.title}"',
        impact: impact,
      ),
    );
    if (ok != true || !mounted) return;
    try {
      await _movieProvider.remove(m.id!);
      showAppSnackBar(context, 'Movie and related data deleted');
      await _load();
    } on ApiClientException catch (e) {
      if (mounted) alertBox(context, 'Cannot delete', e.message);
    } on Exception catch (e) {
      if (mounted) alertBox(context, 'Error', e.toString());
    }
  }

  Future<void> _showMovieDialog({Movie? movie}) async {
    final missing = <String>[
      if (_genres.isEmpty) 'genre',
      if (_ageRatings.isEmpty) 'age rating',
      if (_languages.isEmpty) 'language',
    ];
    if (missing.isNotEmpty) {
      alertBox(
        context,
        'Reference data missing',
        'A movie needs a ${missing.join(', a ')} to be selected. '
            'Add at least one of each under Reference Data first.',
      );
      return;
    }

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
    int? ageRatingId = fullMovie?.ageRatingId;
    int? languageId = fullMovie?.languageId;
    int? genreId = fullMovie?.genreId;
    DateTime? releaseDate = fullMovie?.releaseDate;
    String statusSelection =
        fullMovie?.isActiveState == true ? MovieState.active : MovieState.draft;
    String? posterBase64 = fullMovie?.posterImageBase64;
    String? originalPoster = fullMovie?.posterImageBase64;
    bool submitting = false;
    final formKey = GlobalKey<FormState>();

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
            if (!(formKey.currentState?.validate() ?? false)) return;
            if (genreId == null) {
              showAppSnackBar(context, 'Select a genre', isError: true);
              return;
            }
            setDialogState(() => submitting = true);
            final payload = Movie(
              title: titleCtrl.text.trim(),
              description: '',
              durationMinutes: int.tryParse(durationCtrl.text) ?? 0,
              genreId: genreId,
              releaseDate: releaseDate,
              ageRatingId: ageRatingId,
              languageId: languageId,
            );
            try {
              Movie saved;
              if (movie == null) {
                saved = await _movieProvider.insert(payload.toInsertJson());
                if (statusSelection == MovieState.active) {
                  saved = await _movieProvider.activate(saved.id!);
                }
              } else {
                saved = await _movieProvider.update(movie.id!, payload.toUpdateJson());
                final wantsActive = statusSelection == MovieState.active;
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
                await _load();
              }
            } on Exception catch (e) {
              setDialogState(() => submitting = false);
              if (context.mounted) alertBox(context, 'Error', e.toString());
            }
          },
          child: Form(
            key: formKey,
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
                  child: TextFormField(
                    controller: titleCtrl,
                    decoration: const InputDecoration(labelText: 'Movie Title', hintText: 'Enter movie title'),
                    validator: (v) => FieldValidators.required(v, field: 'Movie title'),
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
                    validator: (v) => v == null ? 'Genre is required' : null,
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: ageRatingId,
                    dropdownColor: AppColors.card,
                    decoration: const InputDecoration(labelText: 'Age Rating'),
                    items: _ageRatings
                        .map((r) => DropdownMenuItem(value: r.id, child: Text(r.name ?? '')))
                        .toList(),
                    onChanged: (v) => setDialogState(() => ageRatingId = v),
                    validator: (v) => v == null ? 'Age rating is required' : null,
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
              DropdownButtonFormField<int>(
                initialValue: languageId,
                dropdownColor: AppColors.card,
                decoration: const InputDecoration(labelText: 'Language'),
                items: _languages
                    .map((l) => DropdownMenuItem(value: l.id, child: Text(l.name ?? '')))
                    .toList(),
                onChanged: (v) => setDialogState(() => languageId = v),
                validator: (v) => v == null ? 'Language is required' : null,
              ),
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
                    items: [
                      for (final state in MovieState.all)
                        DropdownMenuItem(value: state, child: Text(state)),
                    ],
                    onChanged: (v) =>
                        setDialogState(() => statusSelection = v ?? MovieState.draft),
                  ),
                ),
              ]),
            ],
            ),
          ),
        ),
      ),
    );
  }
}
