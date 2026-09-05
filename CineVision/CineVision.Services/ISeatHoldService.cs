namespace CineVision.Services
{
    /// <summary>
    /// Keeps pre-payment seat holds honest: releases the ones whose payment window elapsed so
    /// abandoned checkouts stop blocking seats.
    /// </summary>
    public interface ISeatHoldService
    {
        /// <summary>Releases every expired hold on a projection (any customer).</summary>
        Task ReleaseExpiredHoldsAsync(int projectionId);

        /// <summary>
        /// Releases the caller's own unpaid holds on a projection so a restarted checkout is not
        /// blocked by the seats the same customer abandoned a moment ago.
        /// </summary>
        Task ReleaseOwnHoldsAsync(int userId, int projectionId);
    }
}
