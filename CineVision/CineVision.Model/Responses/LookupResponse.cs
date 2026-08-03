namespace CineVision.Model.Responses
{
    /// <summary>
    /// Shared shape of reference (lookup) data rows: screen types, hall statuses,
    /// age ratings and languages.
    /// </summary>
    public abstract class LookupResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>How many records currently reference this row.</summary>
        public int InUseCount { get; set; }

        /// <summary>
        /// False when other records still reference this row, so the admin app can
        /// disable Delete and show <see cref="DeleteBlockedReason"/> instead of failing.
        /// </summary>
        public bool CanDelete { get; set; } = true;

        public string? DeleteBlockedReason { get; set; }
    }
}
