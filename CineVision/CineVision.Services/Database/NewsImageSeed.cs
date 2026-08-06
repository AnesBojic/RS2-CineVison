using Microsoft.EntityFrameworkCore;

namespace CineVision.Services.Database
{
    /// <summary>
    /// Applies placeholder images to seeded news rows when none exist.
    /// Images live in Database/SeedAssets/news-{newsId}.jpg.
    /// </summary>
    public static class NewsImageSeed
    {
        private const int MaxSeededNewsId = 3;

        public static async Task EnsureSeededAsync(CineVisionDbContext db, CancellationToken cancellationToken = default)
        {
            var newsWithoutImage = await db.News
                .Where(n => n.Id <= MaxSeededNewsId)
                .Where(n => n.ImageBase64 == null || n.ImageBase64 == string.Empty)
                .ToListAsync(cancellationToken);

            if (newsWithoutImage.Count == 0)
            {
                return;
            }

            var assetsDir = ResolveSeedAssetsDirectory();
            if (assetsDir == null)
            {
                return;
            }

            var updated = false;
            foreach (var item in newsWithoutImage)
            {
                var path = Path.Combine(assetsDir, $"news-{item.Id}.jpg");
                if (!File.Exists(path))
                {
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                item.ImageBase64 = Convert.ToBase64String(bytes);
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
