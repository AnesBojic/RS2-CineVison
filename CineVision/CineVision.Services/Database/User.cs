using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CineVision.Services.Database
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        public string PasswordSalt { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }
        
        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public string? ProfileImageBase64 { get; set; }

        /// <summary>
        /// PBKDF2/Base64 hash of the emailed 6-digit reset code (never plaintext).
        /// Length 128 fits CryptoService.GenerateHash (20-byte SHA-256 Base64 ≈ 28 chars) with headroom.
        /// </summary>
        [MaxLength(128)]
        public string? PasswordResetCode { get; set; }

        public DateTime? PasswordResetExpiresAt { get; set; }

        /// <summary>Incremented on logout so previously issued JWTs fail validation.</summary>
        public int TokenVersion { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
} 