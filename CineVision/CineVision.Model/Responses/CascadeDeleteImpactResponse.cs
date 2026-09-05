namespace CineVision.Model.Responses
{
    /// <summary>
    /// Preview of related rows that will be permanently removed by a cascade delete.
    /// </summary>
    public class CascadeDeleteImpactResponse
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Sum of all child rows that will be deleted (excludes the root entity itself).</summary>
        public int TotalAffectedRows { get; set; }

        /// <summary>False when booking or payment history must be preserved.</summary>
        public bool CanDelete { get; set; } = true;

        /// <summary>Why the delete is refused, shown to Admin/Staff instead of a confirm prompt.</summary>
        public string? BlockReason { get; set; }

        public List<CascadeDeleteImpactItem> Items { get; set; } = new();
    }

    public class CascadeDeleteImpactItem
    {
        public string EntityName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
