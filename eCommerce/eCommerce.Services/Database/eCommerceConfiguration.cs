using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateConfiguration(ModelBuilder modelBuilder)
        {
            // A movie belongs to an optional genre; deleting a genre must not cascade-delete movies.
            modelBuilder.Entity<Movie>()
                .HasOne(m => m.Genre)
                .WithMany(g => g.Movies)
                .HasForeignKey(m => m.GenreId)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<UserNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
