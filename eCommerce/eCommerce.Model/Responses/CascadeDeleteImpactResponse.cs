namespace eCommerce.Model.Responses
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

        public List<CascadeDeleteImpactItem> Items { get; set; } = new();
    }

    public class CascadeDeleteImpactItem
    {
        public string EntityName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
