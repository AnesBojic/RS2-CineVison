import 'package:ecommerce_mobile/core/components/movie_card.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/models/genre.dart';
import 'package:ecommerce_mobile/models/movie.dart';
import 'package:ecommerce_mobile/models/recommendation.dart';
import 'package:ecommerce_mobile/models/search_result.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/genre_provider.dart';
import 'package:ecommerce_mobile/providers/movie_provider.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

/// Step 1 — browse & search movies (mockup c1).
class MoviesPage extends StatefulWidget {
  const MoviesPage({super.key});

  @override
  State<MoviesPage> createState() => _MoviesPageState();
}

class _MoviesPageState extends State<MoviesPage> {
  late MovieProvider _movieProvider;
  late GenreProvider _genreProvider;

  SearchResult<Movie>? _movieResult;
  Map<int, String> _recommendationReasons = {};
  List<Genre> _genres = [];
  bool _isLoading = true;
  bool _usingRecommendations = false;
  bool _coldStartRecommendations = false;
  bool? _wasUsingRecommendations;
  int? _selectedGenreId;

  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _movieProvider = context.read<MovieProvider>();
    _genreProvider = context.read<GenreProvider>();
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  bool _canUseRecommendations(AuthProvider auth) {
    return auth.isAuthenticated &&
        AuthProvider.accesstoken != null &&
        AuthProvider.accesstoken!.isNotEmpty;
  }

  List<Movie> _applyClientFilters(List<Movie> movies) {
    final query = _searchController.text.trim().toLowerCase();
    return movies.where((movie) {
      if (_selectedGenreId != null && movie.genreId != _selectedGenreId) {
        return false;
      }
      if (query.isEmpty) return true;
      return (movie.title ?? '').toLowerCase().contains(query);
    }).toList();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final genres = await _genreProvider.get(filter: {'pageSize': 100});

      SearchResult<Movie> movies;
      Map<int, String> reasons = {};
      var usingRecommendations = false;
      var coldStart = false;

      if (_canUseRecommendations(auth)) {
        try {
          final personalized = await _loadPersonalizedCatalog();
          movies = personalized.movies;
          reasons = personalized.reasons;
          usingRecommendations = true;
          coldStart = personalized.coldStart;
        } on Exception {
          movies = await _loadPopularCatalog();
        }
      } else {
        movies = await _loadPopularCatalog();
      }

      if (!mounted) return;
      setState(() {
        _genres = genres.items ?? [];
        _movieResult = movies;
        _recommendationReasons = reasons;
        _usingRecommendations = usingRecommendations;
        _coldStartRecommendations = coldStart;
        _isLoading = false;
      });
    } on Exception catch (e) {
      if (!mounted) return;
      setState(() => _isLoading = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  Future<({SearchResult<Movie> movies, Map<int, String> reasons, bool coldStart})>
      _loadPersonalizedCatalog() async {
    final catalog = await _loadCatalogMovies();
    final items = List<Movie>.from(catalog.items ?? []);

    final recommendations = await _movieProvider.getRecommendations(take: 0);
    final scoreById = <int, Recommendation>{
      for (final r in recommendations)
        if (r.movie.id != null) r.movie.id!: r,
    };

    items.sort((a, b) {
      final scoreA = scoreById[a.id]?.score ?? 0;
      final scoreB = scoreById[b.id]?.score ?? 0;
      final cmp = scoreB.compareTo(scoreA);
      if (cmp != 0) return cmp;
      return (a.title ?? '').compareTo(b.title ?? '');
    });

    // No bookings/reviews yet → API scores are popularity-only (contentScore stays 0).
    final coldStart = recommendations.isEmpty ||
        recommendations.every((r) => r.contentScore <= 0);

    final reasons = <int, String>{};
    for (final r in recommendations) {
      final id = r.movie.id;
      if (id == null || r.reason.trim().isEmpty) continue;
      // Cold start: banner explains popularity; skip repeating it on every card.
      if (coldStart) continue;
      reasons[id] = r.reason;
    }

    final filtered = _applyClientFilters(items);

    return (
      movies: SearchResult<Movie>()
        ..items = filtered
        ..totalCount = filtered.length,
      reasons: reasons,
      coldStart: coldStart,
    );
  }

  Future<SearchResult<Movie>> _loadPopularCatalog() async {
    final catalog = await _loadCatalogMovies();
    final items = List<Movie>.from(catalog.items ?? [])
      ..sort((a, b) => (b.viewCount ?? 0).compareTo(a.viewCount ?? 0));

    final filtered = _applyClientFilters(items);

    return SearchResult<Movie>()
      ..items = filtered
      ..totalCount = filtered.length;
  }

  void _onAuthModeChanged(bool useRecommendations) {
    if (_wasUsingRecommendations == useRecommendations) return;
    _wasUsingRecommendations = useRecommendations;

    if (!mounted) return;

    setState(() {
      _recommendationReasons = {};
      _usingRecommendations = false;
      _coldStartRecommendations = false;
      _movieResult = null;
      _isLoading = true;
    });
    _loadData();
  }

  String get _recommendationBannerText {
    if (_coldStartRecommendations) {
      return 'New account — movies are ranked by popularity. After you book or review, the list adapts to your taste.';
    }
    return 'All movies shown — titles matching your bookings and preferences are ranked higher.';
  }

  Future<SearchResult<Movie>> _loadCatalogMovies() {
    return _movieProvider.get(
      filter: {
        'title': _searchController.text,
        'movieState': 'ActiveMovieState',
        'includeGenre': true,
        'includeAssets': true,
        'pageSize': 100,
        if (_selectedGenreId != null) 'genreId': _selectedGenreId,
      },
      includePoster: true,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        final useRecommendations = _canUseRecommendations(auth);
        if (_wasUsingRecommendations == null) {
          _wasUsingRecommendations = useRecommendations;
        } else if (_wasUsingRecommendations != useRecommendations) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            _onAuthModeChanged(useRecommendations);
          });
        }

        return Scaffold(
          appBar: CineAppBar(
            title: 'Cinevision',
            showBack: false,
            additionalActions: auth.isAuthenticated
                ? [
                    TextButton(
                      onPressed: () => Navigator.pushNamed(
                        context,
                        AppRoutes.myBookings,
                      ),
                      child: const Text('Bookings'),
                    ),
                  ]
                : [],
          ),
          body: RefreshIndicator(
            onRefresh: _loadData,
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AppDefaults.padding,
                      0,
                      AppDefaults.padding,
                      0,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (_usingRecommendations) ...[
                          const SizedBox(height: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 10,
                            ),
                            decoration: BoxDecoration(
                              color: AppColors.coloredBackground,
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: AppColors.separator),
                            ),
                            child: Row(
                              children: [
                                const Icon(
                                  Icons.auto_awesome,
                                  size: 18,
                                  color: AppColors.primary,
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    _recommendationBannerText,
                                    style: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.copyWith(
                                          color: AppColors.textSecondary,
                                        ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                        const SizedBox(height: 12),
                        TextField(
                          controller: _searchController,
                          decoration: InputDecoration(
                            hintText: 'Search movies...',
                            prefixIcon: const Icon(Icons.search),
                            suffixIcon: _searchController.text.isNotEmpty
                                ? IconButton(
                                    icon: const Icon(Icons.clear),
                                    onPressed: () {
                                      _searchController.clear();
                                      _loadData();
                                    },
                                  )
                                : null,
                          ),
                          onSubmitted: (_) => _loadData(),
                          onChanged: (_) => setState(() {}),
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<int?>(
                          initialValue: _selectedGenreId,
                          decoration: const InputDecoration(
                            hintText: 'All Genres',
                          ),
                          items: [
                            const DropdownMenuItem<int?>(
                              value: null,
                              child: Text('All Genres'),
                            ),
                            ..._genres.map(
                              (g) => DropdownMenuItem<int?>(
                                value: g.id,
                                child: Text(g.name ?? ''),
                              ),
                            ),
                          ],
                          onChanged: (value) {
                            _selectedGenreId = value;
                            _loadData();
                          },
                        ),
                        const SizedBox(height: 16),
                      ],
                    ),
                  ),
                ),
                if (_isLoading)
                  const SliverFillRemaining(
                    child: Center(child: CircularProgressIndicator()),
                  )
                else if ((_movieResult?.items ?? []).isEmpty)
                  const SliverFillRemaining(
                    child: Center(
                      child: Text(
                        'No movies found',
                        style: TextStyle(color: AppColors.textSecondary),
                      ),
                    ),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppDefaults.padding,
                    ),
                    sliver: SliverGrid.builder(
                      itemCount: _movieResult!.items!.length,
                      gridDelegate:
                          const SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        crossAxisSpacing: 12,
                        mainAxisSpacing: 16,
                        childAspectRatio: 0.52,
                      ),
                      itemBuilder: (_, index) {
                        final movie = _movieResult!.items![index];
                        return MovieCard(
                          movie: movie,
                          recommendationReason: _usingRecommendations
                              ? _recommendationReasons[movie.id]
                              : null,
                        );
                      },
                    ),
                  ),
                const SliverToBoxAdapter(child: SizedBox(height: 24)),
              ],
            ),
          ),
        );
      },
    );
  }
}
