using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace eCommerce.Services
{
    /// <summary>
    /// Hybrid movie recommender (project spec §8). Combines a catalog-wide popularity score
    /// (reservations, views and ratings) with a personalized content score (preferred genres and
    /// description-keyword overlap built from the user's reservations and high ratings). The two are
    /// blended into a final score: FinalScore = PopularityWeight * Popularity + ContentWeight * Content.
    /// New users with no history get a pure popularity ("cold start") ranking. Scoring runs in memory
    /// after projecting only the needed columns from EF; no external ML packages are used.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly double _popularityWeight;
        private readonly double _contentWeight;

        // Sub-weights for the two content signals (genre match vs description-keyword overlap).
        private const double GenreSubWeight = 0.6;
        private const double KeywordSubWeight = 0.4;

        // A rating at or above this counts as a "liked" movie for building the taste profile.
        private const int LikedRatingThreshold = 4;

        public RecommendationService(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IAuthenticatedUserAccessor userAccessor,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userAccessor = userAccessor;
            _popularityWeight = ReadWeight(configuration["Recommendations:PopularityWeight"], 0.5);
            _contentWeight = ReadWeight(configuration["Recommendations:ContentWeight"], 0.5);
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

            // Candidate pool: active movies only, with the data needed to build MovieResponse.
            var movies = await _dbContext.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .Include(m => m.Assets)
                .Where(m => m.IsActive)
                .ToListAsync();

            if (movies.Count == 0)
            {
                return new List<RecommendationResponse>();
            }

            // ---- popularity signals (catalog-wide) --------------------------------
            // Reservations: count reserved seats for each movie's screenings, ignoring cancelled
            // reservations (consistent with AnalyticsService, whose "tickets sold" excludes cancelled).
            var reservationCounts = (await _dbContext.ReservationSeats
                    .Where(rs => rs.Reservation.Status != ReservationStatus.Cancelled)
                    .Select(rs => new { rs.Screening.MovieId })
                    .ToListAsync())
                .GroupBy(x => x.MovieId)
                .ToDictionary(g => g.Key, g => (double)g.Count());

            var reviewStats = (await _dbContext.Reviews
                    .Select(r => new { r.MovieId, r.Rating })
                    .ToListAsync())
                .GroupBy(x => x.MovieId)
                .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Rating));

            // ---- user taste profile ----------------------------------------------
            var reservedMovieIds = (await _dbContext.Reservations
                    .Where(r => r.UserId == userId && r.Status != ReservationStatus.Cancelled)
                    .Select(r => r.Screening.MovieId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var likedRatedMovieIds = (await _dbContext.Reviews
                    .Where(r => r.UserId == userId && r.Rating >= LikedRatingThreshold)
                    .Select(r => r.MovieId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            bool coldStart = reservedMovieIds.Count == 0 && likedRatedMovieIds.Count == 0;

            // Genre + keyword profile derived from the movies the user reserved or rated highly.
            var profileMovieIds = new HashSet<int>(reservedMovieIds);
            profileMovieIds.UnionWith(likedRatedMovieIds);

            var genreWeights = new Dictionary<int, int>();
            var profileKeywords = new HashSet<string>();

            if (profileMovieIds.Count > 0)
            {
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

            // ---- per-candidate raw signals ---------------------------------------
            // Score every active movie; preferences boost ranking instead of hiding titles.
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

                    return new Candidate
                    {
                        Movie = m,
                        Reservations = resv,
                        Views = m.ViewCount,
                        Rating = rating,
                        GenreComponent = genreComponent,
                        KeywordComponent = keywordComponent
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
            if (!coldStart)
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

            // ---- final hybrid score -----------------------------------------------
            foreach (var c in candidates)
            {
                c.FinalScore = coldStart
                    ? c.PopularityScore
                    : _popularityWeight * c.PopularityScore + _contentWeight * c.ContentScore;
            }

            var ordered = candidates
                .OrderByDescending(c => c.FinalScore)
                .ThenByDescending(c => c.PopularityScore);

            IEnumerable<Candidate> result = take > 0 ? ordered.Take(take) : ordered;

            return result
                .Select(c => new RecommendationResponse
                {
                    Movie = _mapper.Map<MovieResponse>(c.Movie),
                    Score = Math.Round(c.FinalScore, 4),
                    PopularityScore = Math.Round(c.PopularityScore, 4),
                    ContentScore = Math.Round(c.ContentScore, 4),
                    Reason = BuildReason(c, coldStart, reservedMovieIds.Contains(c.Movie.Id))
                })
                .ToList();
        }

        private static string BuildReason(Candidate c, bool coldStart, bool alreadyBooked)
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

        // Min-max normalization to 0-1. Returns 0 when the signal has no spread across candidates.
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
            public double ContentRaw { get; set; }
            public double PopularityScore { get; set; }
            public double ContentScore { get; set; }
            public double FinalScore { get; set; }
        }
    }
}
