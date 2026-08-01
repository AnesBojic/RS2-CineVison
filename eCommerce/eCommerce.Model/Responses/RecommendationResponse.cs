namespace eCommerce.Model.Responses
{
    /// <summary>
    /// A single hybrid movie recommendation: the movie plus the popularity/content/search scores
    /// that produced it and a short human-readable explanation.
    /// </summary>
    public class RecommendationResponse
    {
        public MovieResponse Movie { get; set; } = new MovieResponse();

        /// <summary>Final hybrid score (0-1), the value used for ordering.</summary>
        public double Score { get; set; }

        /// <summary>Catalog-wide popularity component (0-1).</summary>
        public double PopularityScore { get; set; }

        /// <summary>Personalized content-similarity component (0-1).</summary>
        public double ContentScore { get; set; }

        /// <summary>Affinity from the user's recent SearchHistories (0-1).</summary>
        public double SearchScore { get; set; }

        /// <summary>Short explanation, e.g. "Popular + matches your interest in Sci-Fi".</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
