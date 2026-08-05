using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    /// <summary>
    /// Hybrid recommender: popularity + content affinity + search history
    /// (weights from config). Cold start falls back to popularity only.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly CineVisionDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly double _popularityWeight;
        private readonly double _contentWeight;
        private readonly double _searchWeight;

        private const double GenreSubWeight = 0.6;
        private const double KeywordSubWeight = 0.4;
        private const double SearchGenreSubWeight = 0.5;
        private const double SearchKeywordSubWeight = 0.3;
        private const double SearchTitleSubWeight = 0.2;

        private const int LikedRatingThreshold = 4;
        private const int MaxRecentSearches = 40;

        public RecommendationService(
            CineVisionDbContext dbContext,
            IMapper mapper,
            IAuthenticatedUserAccessor userAccessor,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userAccessor = userAccessor;
            _popularityWeight = ReadWeight(configuration["Recommendations:PopularityWeight"], 0.4);
            _contentWeight = ReadWeight(configuration["Recommendations:ContentWeight"], 0.4);
            _searchWeight = ReadWeight(configuration["Recommendations:SearchWeight"], 0.2);
        }

        private static double ReadWeight(string? raw, double fallback)
        {
            return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        public async Task<List<RecommendationResponse>> GetRecommendationsAsync(int take = 10)
        {
            if (take <= 0)
            {
                take = 10;
            }

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            var movies = await _dbContext.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .Include(m => m.Language)
                .Include(m => m.AgeRating)
                .Where(m => m.IsActive)
                .ToListAsync();

            if (movies.Count == 0)
            {
                return new List<RecommendationResponse>();
            }

            // ---- popularity signals (catalog-wide) --------------------------------
            // Aggregate in SQL. Pulling every reservation/review row into memory just to
            // GroupBy is the anti-pattern the performance rules call out.
            var reservationCounts = await _dbContext.ReservationSeats
                .AsNoTracking()
                .Where(rs => rs.Reservation.Status != ReservationStatus.Cancelled)
                .GroupBy(rs => rs.Projection.MovieId)
                .Select(g => new { MovieId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.MovieId, x => (double)x.Count);

            var reviewStats = await _dbContext.Reviews
                .AsNoTracking()
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(x => (double)x.Rating) })
                .ToDictionaryAsync(x => x.MovieId, x => x.Avg);

            // ---- user taste profile (bookings + high ratings) ---------------------
            var reservedMovieIds = (await _dbContext.Reservations
                    .AsNoTracking()
                    .Where(r => r.UserId == userId && r.Status != ReservationStatus.Cancelled)
                    .Select(r => r.Projection.MovieId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var likedRatedMovieIds = (await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(r => r.UserId == userId && r.Rating >= LikedRatingThreshold)
                    .Select(r => r.MovieId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var hasContentProfile = reservedMovieIds.Count > 0 || likedRatedMovieIds.Count > 0;

            var genreWeights = new Dictionary<int, int>();
            var profileKeywords = new HashSet<string>();

            if (hasContentProfile)
            {
                var profileMovieIds = new HashSet<int>(reservedMovieIds);
                profileMovieIds.UnionWith(likedRatedMovieIds);

                var profileMovies = await _dbContext.Movies
                    .AsNoTracking()
                    .Where(m => profileMovieIds.Contains(m.Id))
                    .Select(m => new { m.GenreId, m.Description })
                    .ToListAsync();

                foreach (var pm in profileMovies)
                {
                    if (pm.GenreId.HasValue)
                    {
                        genreWeights.TryGetValue(pm.GenreId.Value, out var count);
                        genreWeights[pm.GenreId.Value] = count + 1;
                    }
                    profileKeywords.UnionWith(Tokenize(pm.Description));
                }
            }

            int maxGenreWeight = genreWeights.Count > 0 ? genreWeights.Values.Max() : 0;

            // ---- search-history profile (must be used — rows are written on search) -
            var recentSearches = await _dbContext.SearchHistories
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SearchedAt)
                .Take(MaxRecentSearches)
                .Select(s => new { s.Query, s.GenreId, s.SearchedAt })
                .ToListAsync();

            var hasSearchProfile = recentSearches.Count > 0;
            var searchGenreWeights = new Dictionary<int, int>();
            var searchKeywords = new HashSet<string>();
            var searchTitlePhrases = new List<string>();

            foreach (var search in recentSearches)
            {
                int? genreId = search.GenreId;
                if (!genreId.HasValue &&
                    search.Query.StartsWith("genre:", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(search.Query.AsSpan("genre:".Length), out var parsedGenreId))
                {
                    genreId = parsedGenreId;
                }

                if (genreId.HasValue)
                {
                    searchGenreWeights.TryGetValue(genreId.Value, out var count);
                    searchGenreWeights[genreId.Value] = count + 1;
                }

                // Synthetic genre-only queries contribute genre affinity, not keyword noise.
                if (!search.Query.StartsWith("genre:", StringComparison.OrdinalIgnoreCase))
                {
                    searchKeywords.UnionWith(Tokenize(search.Query));
                    var phrase = search.Query.Trim().ToLowerInvariant();
                    if (phrase.Length >= 2)
                    {
                        searchTitlePhrases.Add(phrase);
                    }
                }
            }

            int maxSearchGenreWeight = searchGenreWeights.Count > 0 ? searchGenreWeights.Values.Max() : 0;

            // Cold start only when the user has nothing personal to go on — including no searches.
            bool coldStart = !hasContentProfile && !hasSearchProfile;

            // ---- per-candidate raw signals ---------------------------------------
            var candidates = movies
                .Select(m =>
                {
                    reservationCounts.TryGetValue(m.Id, out var resv);
                    reviewStats.TryGetValue(m.Id, out var rating);

                    double genreComponent = 0;
                    if (m.GenreId.HasValue && maxGenreWeight > 0 && genreWeights.TryGetValue(m.GenreId.Value, out var gw))
                    {
                        genreComponent = (double)gw / maxGenreWeight;
                    }

                    double keywordComponent = Jaccard(Tokenize(m.Description), profileKeywords);

                    double searchGenreComponent = 0;
                    if (m.GenreId.HasValue && maxSearchGenreWeight > 0 &&
                        searchGenreWeights.TryGetValue(m.GenreId.Value, out var sgw))
                    {
                        searchGenreComponent = (double)sgw / maxSearchGenreWeight;
                    }

                    double searchKeywordComponent = Jaccard(Tokenize(m.Title), searchKeywords);
                    if (searchKeywordComponent == 0)
                    {
                        // Fall back to description tokens when the title has no overlap.
                        searchKeywordComponent = Jaccard(Tokenize(m.Description), searchKeywords);
                    }

                    double searchTitleComponent = 0;
                    if (searchTitlePhrases.Count > 0 && !string.IsNullOrWhiteSpace(m.Title))
                    {
                        var title = m.Title.ToLowerInvariant();
                        searchTitleComponent = searchTitlePhrases.Any(p => title.Contains(p)) ? 1.0 : 0.0;
                    }

                    return new Candidate
                    {
                        Movie = m,
                        Reservations = resv,
                        Views = m.ViewCount,
                        Rating = rating,
                        GenreComponent = genreComponent,
                        KeywordComponent = keywordComponent,
                        SearchGenreComponent = searchGenreComponent,
                        SearchKeywordComponent = searchKeywordComponent,
                        SearchTitleComponent = searchTitleComponent
                    };
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return new List<RecommendationResponse>();
            }

            // ---- normalize popularity signals and combine -------------------------
            double minResv = candidates.Min(c => c.Reservations), maxResv = candidates.Max(c => c.Reservations);
            double minViews = candidates.Min(c => c.Views), maxViews = candidates.Max(c => c.Views);
            double minRating = candidates.Min(c => c.Rating), maxRating = candidates.Max(c => c.Rating);

            foreach (var c in candidates)
            {
                double nResv = Normalize(c.Reservations, minResv, maxResv);
                double nViews = Normalize(c.Views, minViews, maxViews);
                double nRating = Normalize(c.Rating, minRating, maxRating);
                c.PopularityScore = (nResv + nViews + nRating) / 3.0;
            }

            // ---- normalize content signals ----------------------------------------
            if (hasContentProfile)
            {
                foreach (var c in candidates)
                {
                    c.ContentRaw = GenreSubWeight * c.GenreComponent + KeywordSubWeight * c.KeywordComponent;
                }

                double minContent = candidates.Min(c => c.ContentRaw), maxContent = candidates.Max(c => c.ContentRaw);
                foreach (var c in candidates)
                {
                    c.ContentScore = Normalize(c.ContentRaw, minContent, maxContent);
                }
            }

            // ---- normalize search-history signals ---------------------------------
            if (hasSearchProfile)
            {
                foreach (var c in candidates)
                {
                    c.SearchRaw =
                        SearchGenreSubWeight * c.SearchGenreComponent +
                        SearchKeywordSubWeight * c.SearchKeywordComponent +
                        SearchTitleSubWeight * c.SearchTitleComponent;
                }

                double minSearch = candidates.Min(c => c.SearchRaw), maxSearch = candidates.Max(c => c.SearchRaw);
                foreach (var c in candidates)
                {
                    c.SearchScore = Normalize(c.SearchRaw, minSearch, maxSearch);
                }
            }

            // ---- final hybrid score -----------------------------------------------
            foreach (var c in candidates)
            {
                if (coldStart)
                {
                    c.FinalScore = c.PopularityScore;
                }
                else
                {
                    c.FinalScore =
                        _popularityWeight * c.PopularityScore +
                        _contentWeight * c.ContentScore +
                        _searchWeight * c.SearchScore;
                }
            }

            var ordered = candidates
                .OrderByDescending(c => c.FinalScore)
                .ThenByDescending(c => c.PopularityScore)
                .ThenByDescending(c => c.SearchScore);

            IEnumerable<Candidate> result = take > 0 ? ordered.Take(take) : ordered;

            return result
                .Select(c => new RecommendationResponse
                {
                    Movie = _mapper.Map<MovieResponse>(c.Movie),
                    Score = Math.Round(c.FinalScore, 4),
                    PopularityScore = Math.Round(c.PopularityScore, 4),
                    ContentScore = Math.Round(c.ContentScore, 4),
                    SearchScore = Math.Round(c.SearchScore, 4),
                    Reason = BuildReason(c, coldStart, hasSearchProfile, reservedMovieIds.Contains(c.Movie.Id))
                })
                .ToList();
        }

        private static string BuildReason(Candidate c, bool coldStart, bool hasSearchProfile, bool alreadyBooked)
        {
            var parts = new List<string>();

            if (alreadyBooked)
            {
                parts.Add("already in your bookings");
            }

            if (coldStart)
            {
                parts.Add("popular right now");
            }
            else
            {
                if (c.GenreComponent > 0 && !string.IsNullOrWhiteSpace(c.Movie.Genre?.Name))
                {
                    parts.Add($"matches your interest in {c.Movie.Genre!.Name}");
                }
                if (c.KeywordComponent > 0)
                {
                    parts.Add("similar to movies you've enjoyed");
                }
                if (hasSearchProfile &&
                    (c.SearchGenreComponent > 0 || c.SearchKeywordComponent > 0 || c.SearchTitleComponent > 0))
                {
                    parts.Add("matches your recent searches");
                }
                if (c.PopularityScore >= 0.5)
                {
                    parts.Add("popular with other viewers");
                }
                if (parts.Count == 0)
                {
                    parts.Add("recommended for you");
                }
            }

            var reason = string.Join(" + ", parts);
            return char.ToUpperInvariant(reason[0]) + reason.Substring(1);
        }

        private static double Normalize(double value, double min, double max)
        {
            return max > min ? (value - min) / (max - min) : 0;
        }

        private static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0)
            {
                return 0;
            }

            int intersection = a.Count(b.Contains);
            int union = a.Count + b.Count - intersection;
            return union == 0 ? 0 : (double)intersection / union;
        }

        private static HashSet<string> Tokenize(string? text)
        {
            var tokens = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return tokens;
            }

            foreach (var raw in Regex.Split(text.ToLowerInvariant(), "[^a-z]+"))
            {
                if (raw.Length >= 3 && !StopWords.Contains(raw))
                {
                    tokens.Add(raw);
                }
            }

            return tokens;
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "with", "that", "this", "from", "into", "against", "his", "her",
            "its", "their", "they", "she", "him", "who", "whom", "one", "two", "out", "off", "but",
            "are", "was", "were", "has", "had", "have", "gets", "get", "same", "final", "shot",
            "way", "all", "any", "not", "own", "new", "old", "you", "your"
        };

        private class Candidate
        {
            public Movie Movie { get; set; } = null!;
            public double Reservations { get; set; }
            public double Views { get; set; }
            public double Rating { get; set; }
            public double GenreComponent { get; set; }
            public double KeywordComponent { get; set; }
            public double SearchGenreComponent { get; set; }
            public double SearchKeywordComponent { get; set; }
            public double SearchTitleComponent { get; set; }
            public double ContentRaw { get; set; }
            public double SearchRaw { get; set; }
            public double PopularityScore { get; set; }
            public double ContentScore { get; set; }
            public double SearchScore { get; set; }
            public double FinalScore { get; set; }
        }
    }
}
