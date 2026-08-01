using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateConfiguration(ModelBuilder modelBuilder)
        {
            // Reference (lookup) tables: names are unique so staff cannot create duplicates.
            modelBuilder.Entity<Genre>().HasIndex(g => g.Name).IsUnique();
            modelBuilder.Entity<ScreenType>().HasIndex(s => s.Name).IsUnique();
            modelBuilder.Entity<HallStatus>().HasIndex(s => s.Name).IsUnique();
            modelBuilder.Entity<AgeRating>().HasIndex(a => a.Name).IsUnique();
            modelBuilder.Entity<Language>().HasIndex(l => l.Name).IsUnique();

            // A movie belongs to an optional genre; deleting a genre must not cascade-delete movies.
            modelBuilder.Entity<Movie>()
                .HasOne(m => m.Genre)
                .WithMany(g => g.Movies)
                .HasForeignKey(m => m.GenreId)
                .OnDelete(DeleteBehavior.SetNull);

            // Age rating / language are optional descriptors; clearing the lookup row must not
            // delete movies, so the FK is set to NULL instead.
            modelBuilder.Entity<Movie>()
                .HasOne(m => m.AgeRating)
                .WithMany(a => a.Movies)
                .HasForeignKey(m => m.AgeRatingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Movie>()
                .HasOne(m => m.Language)
                .WithMany(l => l.Movies)
                .HasForeignKey(m => m.LanguageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Screening>()
                .HasOne(s => s.Language)
                .WithMany(l => l.Screenings)
                .HasForeignKey(s => s.LanguageId)
                .OnDelete(DeleteBehavior.SetNull);

            // A hall must always have a screen type and a status; Restrict makes the database
            // reject deleting a lookup row that halls still reference.
            modelBuilder.Entity<Hall>()
                .HasOne(h => h.ScreenType)
                .WithMany(s => s.Halls)
                .HasForeignKey(h => h.ScreenTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Hall>()
                .HasOne(h => h.Status)
                .WithMany(s => s.Halls)
                .HasForeignKey(h => h.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Posters/assets are removed together with their movie.
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Movie)
                .WithMany(m => m.Assets)
                .HasForeignKey(a => a.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seats belong to a hall and are removed together with it.
            modelBuilder.Entity<Seat>()
                .HasOne(s => s.Hall)
                .WithMany(h => h.Seats)
                .HasForeignKey(s => s.HallId)
                .OnDelete(DeleteBehavior.Cascade);

            // A seat is unique within a hall by its row + number.
            modelBuilder.Entity<Seat>()
                .HasIndex(s => new { s.HallId, s.RowLabel, s.SeatNumber })
                .IsUnique();

            modelBuilder.Entity<Seat>()
                .HasOne(s => s.PartnerSeat)
                .WithMany()
                .HasForeignKey(s => s.PartnerSeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Screening relationships. Restrict deletes to avoid multiple cascade paths.
            modelBuilder.Entity<Screening>()
                .HasOne(s => s.Movie)
                .WithMany(m => m.Screenings)
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Screening>()
                .HasOne(s => s.Hall)
                .WithMany(h => h.Screenings)
                .HasForeignKey(s => s.HallId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation relationships.
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Screening)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.ScreeningId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.CancelledByUser)
                .WithMany()
                .HasForeignKey(r => r.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent the same Stripe PaymentIntent from paying multiple reservations.
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.PaymentTransactionId)
                .IsUnique()
                .HasFilter("[PaymentTransactionId] IS NOT NULL");

            // ReservationSeat relationships.
            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Reservation)
                .WithMany(r => r.ReservationSeats)
                .HasForeignKey(rs => rs.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Seat)
                .WithMany(s => s.ReservationSeats)
                .HasForeignKey(rs => rs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationSeat>()
                .HasOne(rs => rs.Screening)
                .WithMany(s => s.ReservationSeats)
                .HasForeignKey(rs => rs.ScreeningId)
                .OnDelete(DeleteBehavior.Restrict);

            // Double-booking prevention: a seat can only be taken once per screening.
            modelBuilder.Entity<ReservationSeat>()
                .HasIndex(rs => new { rs.ScreeningId, rs.SeatId })
                .IsUnique();

            // Review relationships. Deleting a movie removes its reviews; the author FK is
            // restricted to avoid a second cascade path into Review (SQL Server rejects those).
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // A user may review a given movie only once.
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.MovieId })
                .IsUnique();

            // User role relationships (carried over from the template).
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(ur => ur.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(ur => ur.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Refresh tokens die with their user; configured explicitly rather than by convention
            // so every relationship in the model is declared in one place.
            modelBuilder.Entity<RefreshToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SearchHistory>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SearchHistory>()
                .HasOne(s => s.Genre)
                .WithMany()
                .HasForeignKey(s => s.GenreId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SearchHistory>()
                .HasIndex(s => new { s.UserId, s.SearchedAt });
        }
    }
}
