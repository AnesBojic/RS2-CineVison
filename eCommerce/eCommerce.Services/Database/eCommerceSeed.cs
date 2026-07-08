using eCommerce.Services.MovieStateMachine;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateSeed(ModelBuilder modelBuilder)
        {
            SeedGenres(modelBuilder);
            SeedHalls(modelBuilder);
            SeedSeats(modelBuilder);
            SeedMovies(modelBuilder);
            SeedScreenings(modelBuilder);
            SeedRoles(modelBuilder);
            SeedUsers(modelBuilder);
            SeedUserRoles(modelBuilder);
            SeedReviews(modelBuilder);
        }

        private static readonly DateTime SeedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private void SeedGenres(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Genre>().HasData(
                new { Id = 1, Name = "Action", Description = "High-energy action films", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "Drama", Description = "Character-driven dramatic stories", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, Name = "Comedy", Description = "Light-hearted and funny films", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, Name = "Sci-Fi", Description = "Science fiction and futuristic stories", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 5, Name = "Horror", Description = "Suspense and horror films", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedHalls(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hall>().HasData(
                new { Id = 1, Name = "Hall A", Description = "Main auditorium with 40 seats", ScreenType = ScreenType.IMAX, Status = HallStatus.Active, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "Hall B", Description = "Smaller auditorium with 24 seats", ScreenType = ScreenType.Standard, Status = HallStatus.Active, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedSeats(ModelBuilder modelBuilder)
        {
            var seats = new List<object>();
            int seatId = 1;

            // Hall A: rows A-E, 8 seats per row (last row VIP)
            foreach (var row in new[] { "A", "B", "C", "D", "E" })
            {
                for (int n = 1; n <= 8; n++)
                {
                    seats.Add(new
                    {
                        Id = seatId++,
                        HallId = 1,
                        RowLabel = row,
                        SeatNumber = n,
                        SeatType = row == "E" ? SeatType.VIP : SeatType.Regular,
                        IsActive = true
                    });
                }
            }

            // Hall B: rows A-D, 6 seats per row (last row VIP)
            foreach (var row in new[] { "A", "B", "C", "D" })
            {
                for (int n = 1; n <= 6; n++)
                {
                    seats.Add(new
                    {
                        Id = seatId++,
                        HallId = 2,
                        RowLabel = row,
                        SeatNumber = n,
                        SeatType = row == "D" ? SeatType.VIP : SeatType.Regular,
                        IsActive = true
                    });
                }
            }

            modelBuilder.Entity<Seat>().HasData(seats);
        }

        private void SeedMovies(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie>().HasData(
                new { Id = 1, Title = "Edge of Tomorrow", Description = "A soldier relives the same brutal battle in a loop against an alien invasion.", DurationMinutes = 113, Director = "Doug Liman", ReleaseDate = (DateTime?)new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "PG-13", TrailerUrl = (string?)null, IsActive = true, ViewCount = 320, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(ActiveMovieState), GenreId = (int?)4 },
                new { Id = 2, Title = "The Last Laugh", Description = "An ageing comedian gets one final shot at the spotlight.", DurationMinutes = 98, Director = "Greta Park", ReleaseDate = (DateTime?)new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "PG", TrailerUrl = (string?)null, IsActive = true, ViewCount = 140, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(ActiveMovieState), GenreId = (int?)3 },
                new { Id = 3, Title = "Silent Shadows", Description = "A family moves into a house that hides a terrifying secret.", DurationMinutes = 105, Director = "Mark Reyes", ReleaseDate = (DateTime?)new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "R", TrailerUrl = (string?)null, IsActive = true, ViewCount = 210, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(ActiveMovieState), GenreId = (int?)5 },
                new { Id = 4, Title = "Broken Roads", Description = "Two strangers cross the country and find unexpected friendship.", DurationMinutes = 127, Director = "Lena Holt", ReleaseDate = (DateTime?)new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "PG-13", TrailerUrl = (string?)null, IsActive = true, ViewCount = 90, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(ActiveMovieState), GenreId = (int?)2 },
                new { Id = 5, Title = "Final Strike", Description = "An elite agent races to stop a global catastrophe.", DurationMinutes = 118, Director = "Sam Okafor", ReleaseDate = (DateTime?)new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "PG-13", TrailerUrl = (string?)null, IsActive = true, ViewCount = 260, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(ActiveMovieState), GenreId = (int?)1 },
                new { Id = 6, Title = "Quantum Drift", Description = "A physicist discovers a way to travel between parallel worlds.", DurationMinutes = 134, Director = "Iris Vance", ReleaseDate = (DateTime?)new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc), Language = "English", AgeRating = "PG-13", TrailerUrl = (string?)null, IsActive = true, ViewCount = 60, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = nameof(DraftMovieState), GenreId = (int?)4 }
            );
        }

        private void SeedScreenings(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Screening>().HasData(
                new { Id = 1, MovieId = 1, HallId = 1, StartTime = new DateTime(2026, 7, 5, 18, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 5, 19, 53, 0, DateTimeKind.Utc), BasePrice = 8.50m, Language = "English", HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, MovieId = 1, HallId = 1, StartTime = new DateTime(2026, 7, 5, 21, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 5, 22, 53, 0, DateTimeKind.Utc), BasePrice = 8.50m, Language = "English", HasSubtitles = true, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, MovieId = 2, HallId = 1, StartTime = new DateTime(2026, 7, 6, 17, 30, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 6, 19, 8, 0, DateTimeKind.Utc), BasePrice = 7.00m, Language = "English", HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, MovieId = 3, HallId = 2, StartTime = new DateTime(2026, 7, 6, 20, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 6, 21, 45, 0, DateTimeKind.Utc), BasePrice = 9.00m, Language = "English", HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 5, MovieId = 5, HallId = 2, StartTime = new DateTime(2026, 7, 7, 19, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 7, 20, 58, 0, DateTimeKind.Utc), BasePrice = 10.00m, Language = "English", HasSubtitles = true, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new { Id = 1, Name = "Admin", Description = "Administrator role with full permissions", IsActive = true, CreatedAt = SeedDate },
                new { Id = 2, Name = "Customer", Description = "Default customer role", IsActive = true, CreatedAt = SeedDate },
                new { Id = 3, Name = "Staff", Description = "Employee role for content management and analytics", IsActive = true, CreatedAt = SeedDate }
            );
        }

        private void SeedUsers(ModelBuilder modelBuilder)
        {
            // Seed Users - admin1 (Admin), admin2/admin3 (Staff), customer1/customer2 (Customer). All passwords: Test123
            modelBuilder.Entity<User>().HasData(
                new { Id = 1, FirstName = "Alice", LastName = "Admin", Email = "admin1@gmail.com", Username = "admin1", PasswordHash = "5kRBQg4Ufcx4hAknG7P9zhfLPvY=", PasswordSalt = "FmvmUwPsJyRRffhNRQvbrA==", IsActive = true, CreatedAt = SeedDate, LastLoginAt = (DateTime?)null, PhoneNumber = (string?)null, UpdatedAt = (DateTime?)null },
                new { Id = 2, FirstName = "Bob", LastName = "Staff", Email = "admin2@gmail.com", Username = "admin2", PasswordHash = "GBoyh1WP+OMgGjqRj6vK6L1+oGc=", PasswordSalt = "0AXpKx6xRp9xM42jCf/PiA==", IsActive = true, CreatedAt = SeedDate, LastLoginAt = (DateTime?)null, PhoneNumber = (string?)null, UpdatedAt = (DateTime?)null },
                new { Id = 3, FirstName = "Carol", LastName = "Staff", Email = "admin3@gmail.com", Username = "admin3", PasswordHash = "x6JHKCTQywdAzTcZxGWFvrKPORM=", PasswordSalt = "IwhTfKQNgyqWfOlTqCDXrg==", IsActive = true, CreatedAt = SeedDate, LastLoginAt = (DateTime?)null, PhoneNumber = (string?)null, UpdatedAt = (DateTime?)null },
                new { Id = 4, FirstName = "Dave", LastName = "Customer", Email = "customer1@gmail.com", Username = "customer1", PasswordHash = "E0fA2/f9GZvIRRt/cgqQemG/Cog=", PasswordSalt = "TiJxWTJcd7sBSiWNbhK9Vw==", IsActive = true, CreatedAt = SeedDate, LastLoginAt = (DateTime?)null, PhoneNumber = (string?)null, UpdatedAt = (DateTime?)null },
                new { Id = 5, FirstName = "Eve", LastName = "Customer", Email = "customer2@gmail.com", Username = "customer2", PasswordHash = "Ov4LxpWKXXV9dwMYvBgqODdzIt0=", PasswordSalt = "KtWF6g7SemBqs4nVWV4Ziw==", IsActive = true, CreatedAt = SeedDate, LastLoginAt = (DateTime?)null, PhoneNumber = (string?)null, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedUserRoles(ModelBuilder modelBuilder)
        {
            // admin1 => Admin (1); admin2/admin3 => Staff (3); customer1/customer2 => Customer (2)
            modelBuilder.Entity<UserRole>().HasData(
                new { Id = 1, UserId = 1, RoleId = 1, DateAssigned = SeedDate },
                new { Id = 2, UserId = 2, RoleId = 3, DateAssigned = SeedDate },
                new { Id = 3, UserId = 3, RoleId = 3, DateAssigned = SeedDate },
                new { Id = 4, UserId = 4, RoleId = 2, DateAssigned = SeedDate },
                new { Id = 5, UserId = 5, RoleId = 2, DateAssigned = SeedDate }
            );
        }

        private void SeedReviews(ModelBuilder modelBuilder)
        {
            // Customers customer1 (Id 4) and customer2 (Id 5) rate a handful of active movies.
            // Gives the recommender content/popularity signal without touching any password hashes.
            modelBuilder.Entity<Review>().HasData(
                new { Id = 1, UserId = 4, MovieId = 1, Rating = 5, Comment = (string?)"Loved the relentless time-loop action.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, UserId = 5, MovieId = 1, Rating = 4, Comment = (string?)"A great sci-fi thrill ride.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, UserId = 4, MovieId = 5, Rating = 4, Comment = (string?)"Solid, fast-paced action.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, UserId = 4, MovieId = 2, Rating = 3, Comment = (string?)"A charming but slight comedy.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 5, UserId = 5, MovieId = 3, Rating = 3, Comment = (string?)"Creepy atmosphere, slow middle.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 6, UserId = 5, MovieId = 4, Rating = 5, Comment = (string?)"A heartfelt cross-country journey.", CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }
    }
}
