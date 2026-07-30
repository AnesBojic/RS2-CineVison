using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    /// <summary>
    /// Real search queries performed by users — feeds the recommender (not mock UI history).
    /// </summary>
    public class SearchHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Query { get; set; } = string.Empty;

        public int? GenreId { get; set; }

        [ForeignKey(nameof(GenreId))]
        public Genre? Genre { get; set; }

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    }
}
