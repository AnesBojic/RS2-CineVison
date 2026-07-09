namespace eCommerce.Model.SearchObjects
{
    public class ReviewSearchObject : BaseSearchObject
    {
        /// <summary>Filter reviews for a single movie.</summary>
        public int? MovieId { get; set; }

        /// <summary>Filter reviews written by a single user.</summary>
        public int? UserId { get; set; }
    }
}
