using Microsoft.EntityFrameworkCore;

namespace CineVision.Services.Database
{
    /// <summary>
    /// Applies deterministic placeholder poster images to seeded movies when none exist.
    /// Images live in Database/SeedAssets/poster-{movieId}.jpg (picsum.photos seeds).
    /// </summary>
    public static class MoviePosterSeed
    {
        private const int MaxSeededMovieId = 6;

        public static async Task EnsureSeededAsync(CineVisionDbContext db, CancellationToken cancellationToken = default)
        {
            var moviesWithoutPoster = await db.Movies
                .Where(m => m.Id <= MaxSeededMovieId)
                .Where(m => m.PosterImageBase64 == null || m.PosterImageBase64 == string.Empty)
                .ToListAsync(cancellationToken);

            if (moviesWithoutPoster.Count == 0)
            {
                return;
            }

            var assetsDir = ResolveSeedAssetsDirectory();
            if (assetsDir == null)
            {
                return;
            }

            var updated = false;
            foreach (var movie in moviesWithoutPoster)
            {
                var path = Path.Combine(assetsDir, $"poster-{movie.Id}.jpg");
                if (!File.Exists(path))
                {
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                movie.PosterImageBase64 = Convert.ToBase64String(bytes);
                updated = true;
            }

            if (updated)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private static string? ResolveSeedAssetsDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Database", "SeedAssets"),
                Path.Combine(AppContext.BaseDirectory, "SeedAssets"),
                Path.Combine(Directory.GetCurrentDirectory(), "Database", "SeedAssets"),
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
