import 'package:ecommerce_mobile/core/components/movie_card.dart';
import 'package:ecommerce_mobile/core/constants/app_colors.dart';
import 'package:ecommerce_mobile/core/constants/app_defaults.dart';
import 'package:ecommerce_mobile/core/routes/app_routes.dart';
import 'package:ecommerce_mobile/core/widgets/cine_app_bar.dart';
import 'package:ecommerce_mobile/models/genre.dart';
import 'package:ecommerce_mobile/models/movie.dart';
import 'package:ecommerce_mobile/models/search_result.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/providers/genre_provider.dart';
import 'package:ecommerce_mobile/providers/movie_provider.dart';
import 'package:ecommerce_mobile/providers/notification_provider.dart';
import 'package:ecommerce_mobile/providers/screening_provider.dart';
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
  late ScreeningProvider _screeningProvider;

  final ScrollController _scrollController = ScrollController();
  final TextEditingController _searchController = TextEditingController();

  List<Movie> _movies = [];
  Map<int, String> _recommendationReasons = {};
  List<Genre> _genres = [];
  bool _isLoading = true; // initial load
  bool _isLoadingMore = false;
  bool _hasMore = true;

  bool _usingRecommendations = false;
  bool _coldStartRecommendations = false;
  bool? _wasUsingRecommendations;
  int? _selectedGenreId;

  Set<int> _upcomingMovieIds = {};

  // Pagination (popular mode)
  int _popularPage = 1;
  final int _popularPageSize = 12;
  int? _popularTotalCount;

  // Pagination (recommendations mode)
  int _recommendationsTake = 12;

  @override
  void initState() {
    super.initState();
    _movieProvider = context.read<MovieProvider>();
    _genreProvider = context.read<GenreProvider>();
    _screeningProvider = context.read<ScreeningProvider>();
    _scrollController.addListener(_onScroll);
    _loadData();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final auth = context.read<AuthProvider>();
      if (auth.isAuthenticated) {
        final notifications = context.read<NotificationProvider>();
        notifications.refresh();
        notifications.connectRealtime();
      }
    });
  }

  @override
  void dispose() {
    _scrollController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_isLoading || _isLoadingMore || !_hasMore) return;
    if (!_scrollController.hasClients) return;
    final max = _scrollController.position.maxScrollExtent;
    final current = _scrollController.position.pixels;
    if (current >= max - 300) {
      _loadMore();
    }
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
    if (!mounted) return;
    setState(() {
      _isLoading = true;
      _isLoadingMore = false;
      _hasMore = true;
      _popularPage = 1;
      _popularTotalCount = null;
      _recommendationsTake = 12;
      _movies = [];
      _recommendationReasons = {};
      _upcomingMovieIds = {};
    });

    try {
      final auth = context.read<AuthProvider>();
      // Genres and upcoming-screening ids do not depend on each other.
      final boot = await Future.wait([
        _genreProvider.get(filter: {'pageSize': 100}),
        _fetchUpcomingMovieIds(),
      ]);
      final genres = boot[0] as SearchResult<Genre>;
      final upcomingIds = boot[1] as Set<int>;
      if (!mounted) return;
      setState(() => _upcomingMovieIds = upcomingIds);

      List<Movie> movies = [];
      Map<int, String> reasons = {};
      var usingRecommendations = false;
      var coldStart = false;

      if (_canUseRecommendations(auth) && upcomingIds.isNotEmpty) {
        try {
          final loaded = await _loadRecommendationsForTake(_recommendationsTake);
          movies = loaded.$1;
          reasons = loaded.$2;
          coldStart = loaded.$3;
          usingRecommendations = true;
        } on Exception {
          // fallback to popular mode
          final loaded = await _loadPopularNextPage(reset: true);
          movies = loaded.$1;
          _hasMore = loaded.$2;
          usingRecommendations = false;
          coldStart = false;
        }
      } else if (upcomingIds.isNotEmpty) {
        final loaded = await _loadPopularNextPage(reset: true);
        movies = loaded.$1;
        _hasMore = loaded.$2;
        usingRecommendations = false;
      }

      if (!mounted) return;
      setState(() {
        _genres = genres.items ?? [];
        _movies = movies;
        _recommendationReasons = reasons;
        _usingRecommendations = usingRecommendations;
        _coldStartRecommendations = coldStart;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoading = false);
      alertBox(context, 'Error', e.toString());
    }
  }

  Future<Set<int>> _fetchUpcomingMovieIds() async {
    final ids = <int>{};
    const pageSize = 200;
    var page = 1;
    int? totalCount;

    while (true) {
      final result = await _screeningProvider.get(
        filter: {
          'onlyUpcoming': true,
          'page': page,
          'pageSize': pageSize,
          'includeTotalCount': true,
        },
      );

      totalCount ??= result.totalCount;
      final items = result.items ?? [];

      for (final s in items) {
        final movieId = s.movieId;
        if (movieId != null) ids.add(movieId);
      }

      final loadedCount = page * pageSize;
      if (items.isEmpty || (totalCount != null && loadedCount >= totalCount)) {
        break;
      }
      page++;
    }

    return ids;
  }

  Future<(List<Movie>, Map<int, String>, bool)> _loadRecommendationsForTake(int take) async {
    final recommendations = await _movieProvider.getRecommendations(take: take);

    // Cold start only when the user has neither bookings/ratings nor search history.
    final coldStart = recommendations.isEmpty ||
        recommendations.every((r) => r.contentScore <= 0 && r.searchScore <= 0);

    final reasons = <int, String>{};
    if (!coldStart) {
      for (final r in recommendations) {
        final id = r.movie.id;
        if (id == null || r.reason.trim().isEmpty) continue;
        reasons[id] = r.reason;
      }
    }

    final filtered = recommendations
        .map((r) => r.movie)
        .where((m) => m.id != null && _upcomingMovieIds.contains(m.id))
        .toList();

    final uiFiltered = _applyClientFilters(filtered);
    return (uiFiltered, reasons, coldStart);
  }

  Future<(List<Movie>, bool)> _loadPopularNextPage({required bool reset}) async {
    if (reset) {
      _popularPage = 1;
      _popularTotalCount = null;
      _movies = [];
    }

    final filter = <String, dynamic>{
      'page': _popularPage,
      'pageSize': _popularPageSize,
      'includeTotalCount': true,
      'movieState': MovieState.active,
      'includeGenre': true,
      'includeAssets': true,
      'sortBy': 'ViewCount desc',
      'title': _searchController.text,
      if (_selectedGenreId != null) 'genreId': _selectedGenreId,
    };

    final catalog = await _movieProvider.get(
      filter: filter,
      includePoster: true,
    );

    _popularTotalCount = catalog.totalCount ?? _popularTotalCount;
    final items = catalog.items ?? [];

    // Filter to only movies that have upcoming projections.
    final upcomingFiltered = items
        .where((m) => m.id != null && _upcomingMovieIds.contains(m.id))
        .toList();

    final uiFiltered = _applyClientFilters(upcomingFiltered);

    final rawMoreAvailable = items.isNotEmpty &&
        !(_popularTotalCount != null &&
          (_popularPage * _popularPageSize) >= _popularTotalCount!);

    if (reset) {
      _movies = uiFiltered;
    } else {
      _movies.addAll(uiFiltered);
    }

    _popularPage++;
    return (_movies, rawMoreAvailable);
  }

  void _onAuthModeChanged(bool useRecommendations) {
    if (_wasUsingRecommendations == useRecommendations) return;
    _wasUsingRecommendations = useRecommendations;

    if (!mounted) return;

    setState(() {
      _recommendationReasons = {};
      _usingRecommendations = false;
      _coldStartRecommendations = false;
      _movies = [];
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
            additionalActions: [
              TextButton(
                onPressed: () => Navigator.pushNamed(context, AppRoutes.news),
                child: const Text('News'),
              ),
              if (auth.isAuthenticated) ...[
                Consumer<NotificationProvider>(
                  builder: (context, notifications, _) {
                    final count = notifications.unreadCount;
                    return IconButton(
                      tooltip: 'Notifications',
                      onPressed: () => Navigator.pushNamed(
                        context,
                        AppRoutes.notifications,
                      ),
                      icon: Badge(
                        isLabelVisible: count > 0,
                        label: Text('$count'),
                        child: const Icon(Icons.notifications_outlined),
                      ),
                    );
                  },
                ),
                TextButton(
                  onPressed: () => Navigator.pushNamed(
                    context,
                    AppRoutes.myBookings,
                  ),
                  child: const Text('Bookings'),
                ),
              ],
            ],
          ),
          body: RefreshIndicator(
            onRefresh: _loadData,
            child: CustomScrollView(
              controller: _scrollController,
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
                else if (_movies.isEmpty)
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
                      itemCount: _movies.length,
                      gridDelegate:
                          const SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        crossAxisSpacing: 12,
                        mainAxisSpacing: 16,
                        childAspectRatio: 0.52,
                      ),
                      itemBuilder: (_, index) {
                        final movie = _movies[index];
                        return MovieCard(
                          movie: movie,
                          recommendationReason: _usingRecommendations
                              ? _recommendationReasons[movie.id]
                              : null,
                        );
                      },
                    ),
                  ),
                if (!_isLoading && _isLoadingMore)
                  const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.symmetric(vertical: 16),
                      child: Center(child: CircularProgressIndicator()),
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

  Future<void> _loadMore() async {
    if (_isLoading || _isLoadingMore || !_hasMore) return;
    if (_upcomingMovieIds.isEmpty) return;

    setState(() => _isLoadingMore = true);
    try {
      if (_usingRecommendations) {
        _recommendationsTake += _popularPageSize;
        final loaded = await _loadRecommendationsForTake(_recommendationsTake);

        // Re-check hasMore by asking for the raw recommendation count.
        final raw = await _movieProvider.getRecommendations(take: _recommendationsTake);
        final rawHasMore = raw.isNotEmpty && raw.length >= _recommendationsTake;

        setState(() {
          _movies = loaded.$1;
          _recommendationReasons = loaded.$2;
          _coldStartRecommendations = loaded.$3;
          _hasMore = rawHasMore;
        });
      } else {
        final loaded = await _loadPopularNextPage(reset: false);
        _hasMore = loaded.$2;
      }
    } catch (_) {
      // Keep current list on transient failures.
    } finally {
      if (mounted) setState(() => _isLoadingMore = false);
    }
  }
}
