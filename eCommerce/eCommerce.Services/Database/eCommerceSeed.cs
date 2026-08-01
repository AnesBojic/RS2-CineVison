using eCommerce.Model;
using eCommerce.Services.MovieStateMachine;
using Microsoft.EntityFrameworkCore;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateSeed(ModelBuilder modelBuilder)
        {
            SeedGenres(modelBuilder);
            SeedScreenTypes(modelBuilder);
            SeedHallStatuses(modelBuilder);
            SeedAgeRatings(modelBuilder);
            SeedLanguages(modelBuilder);
            SeedHalls(modelBuilder);
            SeedSeats(modelBuilder);
            SeedMovies(modelBuilder);
            SeedScreenings(modelBuilder);
            SeedRoles(modelBuilder);
            SeedUsers(modelBuilder);
            SeedUserRoles(modelBuilder);
            SeedReviews(modelBuilder);
            SeedReservations(modelBuilder);
            SeedReservationSeats(modelBuilder);
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

        private void SeedScreenTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScreenType>().HasData(
                new { Id = 1, Name = "Standard", Description = "Standard 2D digital projection", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "IMAX", Description = "Large-format IMAX screen", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, Name = "3D", Description = "Stereoscopic 3D projection", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedHallStatuses(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HallStatus>().HasData(
                new { Id = 1, Name = "Active", Description = "Hall is open and can host projections", AllowsScreenings = true, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "Maintenance", Description = "Temporarily closed for maintenance", AllowsScreenings = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, Name = "Inactive", Description = "Permanently out of use", AllowsScreenings = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedAgeRatings(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgeRating>().HasData(
                new { Id = 1, Name = "G", Description = "General audiences — all ages admitted", MinimumAge = (int?)0, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "PG", Description = "Parental guidance suggested", MinimumAge = (int?)8, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, Name = "PG-13", Description = "Some material may be inappropriate for children under 13", MinimumAge = (int?)13, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, Name = "R", Description = "Restricted — under 17 requires an accompanying adult", MinimumAge = (int?)17, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 5, Name = "NC-17", Description = "No one 17 and under admitted", MinimumAge = (int?)18, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedLanguages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Language>().HasData(
                new { Id = 1, Name = "English", Code = (string?)"en", Description = "English audio", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "Bosnian", Code = (string?)"bs", Description = "Bosnian audio", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, Name = "German", Code = (string?)"de", Description = "German audio", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, Name = "Spanish", Code = (string?)"es", Description = "Spanish audio", IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedHalls(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hall>().HasData(
                new { Id = 1, Name = "Hall A", Description = "Main auditorium with 40 seats", ScreenTypeId = 2, StatusId = 1, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, Name = "Hall B", Description = "Smaller auditorium with 24 seats", ScreenTypeId = 1, StatusId = 1, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedSeats(ModelBuilder modelBuilder)
        {
            var seats = new List<object>();
            int seatId = 1;

            // Hall A: rows A-E, 8 seats per row
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
                        SeatType = SeatType.Regular,
                        IsActive = true
                    });
                }
            }

            // Hall B: rows A-D, 6 seats per row
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
                        SeatType = SeatType.Regular,
                        IsActive = true
                    });
                }
            }

            modelBuilder.Entity<Seat>().HasData(seats);
        }

        private void SeedMovies(ModelBuilder modelBuilder)
        {
            // PosterImageBase64 is filled on first API startup by MoviePosterSeed (SeedAssets/poster-{id}.jpg).
            modelBuilder.Entity<Movie>().HasData(
                new { Id = 1, Title = "Edge of Tomorrow", Description = "A soldier relives the same brutal battle in a loop against an alien invasion.", DurationMinutes = 113, Director = "Doug Liman", ReleaseDate = (DateTime?)new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)3, TrailerUrl = (string?)null, IsActive = true, ViewCount = 320, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Active, GenreId = (int?)4 },
                new { Id = 2, Title = "The Last Laugh", Description = "An ageing comedian gets one final shot at the spotlight.", DurationMinutes = 98, Director = "Greta Park", ReleaseDate = (DateTime?)new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)2, TrailerUrl = (string?)null, IsActive = true, ViewCount = 140, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Active, GenreId = (int?)3 },
                new { Id = 3, Title = "Silent Shadows", Description = "A family moves into a house that hides a terrifying secret.", DurationMinutes = 105, Director = "Mark Reyes", ReleaseDate = (DateTime?)new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)4, TrailerUrl = (string?)null, IsActive = true, ViewCount = 210, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Active, GenreId = (int?)5 },
                new { Id = 4, Title = "Broken Roads", Description = "Two strangers cross the country and find unexpected friendship.", DurationMinutes = 127, Director = "Lena Holt", ReleaseDate = (DateTime?)new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)3, TrailerUrl = (string?)null, IsActive = true, ViewCount = 90, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Active, GenreId = (int?)2 },
                new { Id = 5, Title = "Final Strike", Description = "An elite agent races to stop a global catastrophe.", DurationMinutes = 118, Director = "Sam Okafor", ReleaseDate = (DateTime?)new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)3, TrailerUrl = (string?)null, IsActive = true, ViewCount = 260, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Active, GenreId = (int?)1 },
                new { Id = 6, Title = "Quantum Drift", Description = "A physicist discovers a way to travel between parallel worlds.", DurationMinutes = 134, Director = "Iris Vance", ReleaseDate = (DateTime?)new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc), LanguageId = (int?)1, AgeRatingId = (int?)3, TrailerUrl = (string?)null, IsActive = true, ViewCount = 60, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null, MovieState = MovieLifecycleState.Draft, GenreId = (int?)4 }
            );
        }

        private void SeedScreenings(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Screening>().HasData(
                new { Id = 1, MovieId = 1, HallId = 1, StartTime = new DateTime(2026, 7, 5, 18, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 5, 19, 53, 0, DateTimeKind.Utc), BasePrice = 8.50m, LanguageId = (int?)1, HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 2, MovieId = 1, HallId = 1, StartTime = new DateTime(2026, 7, 5, 21, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 5, 22, 53, 0, DateTimeKind.Utc), BasePrice = 8.50m, LanguageId = (int?)1, HasSubtitles = true, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 3, MovieId = 2, HallId = 1, StartTime = new DateTime(2026, 7, 6, 17, 30, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 6, 19, 8, 0, DateTimeKind.Utc), BasePrice = 7.00m, LanguageId = (int?)1, HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 4, MovieId = 3, HallId = 2, StartTime = new DateTime(2026, 7, 6, 20, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 6, 21, 45, 0, DateTimeKind.Utc), BasePrice = 9.00m, LanguageId = (int?)1, HasSubtitles = false, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null },
                new { Id = 5, MovieId = 5, HallId = 2, StartTime = new DateTime(2026, 7, 7, 19, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 7, 20, 58, 0, DateTimeKind.Utc), BasePrice = 10.00m, LanguageId = (int?)1, HasSubtitles = true, IsActive = true, CreatedAt = SeedDate, UpdatedAt = (DateTime?)null }
            );
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new { Id = 1, Name = RoleNames.Admin, Description = "Administrator role with full permissions", IsActive = true, CreatedAt = SeedDate },
                new { Id = 2, Name = RoleNames.Customer, Description = "Default customer role", IsActive = true, CreatedAt = SeedDate },
                new { Id = 3, Name = RoleNames.Staff, Description = "Employee role for content management and analytics", IsActive = true, CreatedAt = SeedDate }
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

        private void SeedReservations(ModelBuilder modelBuilder)
        {
            // customer1 (4) and customer2 (5) purchased tickets across screenings for analytics testing.
            // Paid reservations drive revenue; Confirmed counts toward tickets/occupancy without revenue.
            modelBuilder.Entity<Reservation>().HasData(
                new
                {
                    Id = 1,
                    ReservationNumber = "R-SEED-001",
                    ReservationDate = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 25.50m,
                    UserId = 4,
                    ScreeningId = 1,
                    CustomerName = (string?)"Dave Customer",
                    CustomerEmail = (string?)"customer1@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_001",
                    PaymentDate = (DateTime?)new DateTime(2026, 6, 15, 14, 31, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 2,
                    ReservationNumber = "R-SEED-002",
                    ReservationDate = new DateTime(2026, 6, 20, 10, 15, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 17.00m,
                    UserId = 5,
                    ScreeningId = 1,
                    CustomerName = (string?)"Eve Customer",
                    CustomerEmail = (string?)"customer2@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_002",
                    PaymentDate = (DateTime?)new DateTime(2026, 6, 20, 10, 16, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 3,
                    ReservationNumber = "R-SEED-003",
                    ReservationDate = new DateTime(2026, 6, 22, 18, 45, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 21.00m,
                    UserId = 4,
                    ScreeningId = 3,
                    CustomerName = (string?)"Dave Customer",
                    CustomerEmail = (string?)"customer1@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_003",
                    PaymentDate = (DateTime?)new DateTime(2026, 6, 22, 18, 46, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 4,
                    ReservationNumber = "R-SEED-004",
                    ReservationDate = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 36.00m,
                    UserId = 5,
                    ScreeningId = 4,
                    CustomerName = (string?)"Eve Customer",
                    CustomerEmail = (string?)"customer2@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_004",
                    PaymentDate = (DateTime?)new DateTime(2026, 7, 1, 11, 1, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 5,
                    ReservationNumber = "R-SEED-005",
                    ReservationDate = new DateTime(2026, 7, 3, 16, 20, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 40.00m,
                    UserId = 4,
                    ScreeningId = 5,
                    CustomerName = (string?)"Dave Customer",
                    CustomerEmail = (string?)"customer1@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_005",
                    PaymentDate = (DateTime?)new DateTime(2026, 7, 3, 16, 21, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 6,
                    ReservationNumber = "R-SEED-006",
                    ReservationDate = new DateTime(2026, 6, 28, 20, 5, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Paid,
                    TotalAmount = 34.00m,
                    UserId = 5,
                    ScreeningId = 2,
                    CustomerName = (string?)"Eve Customer",
                    CustomerEmail = (string?)"customer2@gmail.com",
                    PaymentTransactionId = (string?)"pi_seed_006",
                    PaymentDate = (DateTime?)new DateTime(2026, 6, 28, 20, 6, 0, DateTimeKind.Utc),
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                },
                new
                {
                    Id = 7,
                    ReservationNumber = "R-SEED-007",
                    ReservationDate = new DateTime(2026, 6, 30, 9, 40, 0, DateTimeKind.Utc),
                    Status = ReservationStatus.Confirmed,
                    TotalAmount = 8.50m,
                    UserId = 4,
                    ScreeningId = 2,
                    CustomerName = (string?)"Dave Customer",
                    CustomerEmail = (string?)"customer1@gmail.com",
                    PaymentTransactionId = (string?)null,
                    PaymentDate = (DateTime?)null,
                    CancelledByUserId = (int?)null,
                    CancelledAt = (DateTime?)null,
                    CancellationReason = (string?)null,
                    CompletedAt = (DateTime?)null
                }
            );

            modelBuilder.Entity<News>().HasData(
                new
                {
                    Id = 1,
                    Title = "Summer premiere nights",
                    Content = "Join us every Friday for premiere nights with discounted snacks and late shows.",
                    ImageBase64 = (string?)null,
                    PublishedAt = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 2,
                    Title = "Student discount weekdays",
                    Content = "Show your student ID Monday–Thursday for 20% off base ticket prices.",
                    ImageBase64 = (string?)null,
                    PublishedAt = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                }
            );
        }

        private void SeedReservationSeats(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReservationSeat>().HasData(
                // R-SEED-001 — Edge of Tomorrow, 18:00 screening
                new { Id = 1, ReservationId = 1, SeatId = 1, ScreeningId = 1, Price = 8.50m },
                new { Id = 2, ReservationId = 1, SeatId = 2, ScreeningId = 1, Price = 8.50m },
                new { Id = 3, ReservationId = 1, SeatId = 3, ScreeningId = 1, Price = 8.50m },
                // R-SEED-002 — same screening, different seats
                new { Id = 4, ReservationId = 2, SeatId = 4, ScreeningId = 1, Price = 8.50m },
                new { Id = 5, ReservationId = 2, SeatId = 5, ScreeningId = 1, Price = 8.50m },
                // R-SEED-003 — The Last Laugh, afternoon slot
                new { Id = 6, ReservationId = 3, SeatId = 20, ScreeningId = 3, Price = 7.00m },
                new { Id = 7, ReservationId = 3, SeatId = 21, ScreeningId = 3, Price = 7.00m },
                new { Id = 8, ReservationId = 3, SeatId = 22, ScreeningId = 3, Price = 7.00m },
                // R-SEED-004 — Silent Shadows, Hall B
                new { Id = 9, ReservationId = 4, SeatId = 41, ScreeningId = 4, Price = 9.00m },
                new { Id = 10, ReservationId = 4, SeatId = 42, ScreeningId = 4, Price = 9.00m },
                new { Id = 11, ReservationId = 4, SeatId = 43, ScreeningId = 4, Price = 9.00m },
                new { Id = 12, ReservationId = 4, SeatId = 44, ScreeningId = 4, Price = 9.00m },
                // R-SEED-005 — Final Strike
                new { Id = 13, ReservationId = 5, SeatId = 47, ScreeningId = 5, Price = 10.00m },
                new { Id = 14, ReservationId = 5, SeatId = 48, ScreeningId = 5, Price = 10.00m },
                new { Id = 15, ReservationId = 5, SeatId = 49, ScreeningId = 5, Price = 10.00m },
                new { Id = 16, ReservationId = 5, SeatId = 50, ScreeningId = 5, Price = 10.00m },
                // R-SEED-006 — Edge of Tomorrow, 21:00 screening (9 PM time slot)
                new { Id = 17, ReservationId = 6, SeatId = 9, ScreeningId = 2, Price = 8.50m },
                new { Id = 18, ReservationId = 6, SeatId = 10, ScreeningId = 2, Price = 8.50m },
                new { Id = 19, ReservationId = 6, SeatId = 11, ScreeningId = 2, Price = 8.50m },
                new { Id = 20, ReservationId = 6, SeatId = 12, ScreeningId = 2, Price = 8.50m },
                // R-SEED-007 — unpaid hold on late screening
                new { Id = 21, ReservationId = 7, SeatId = 13, ScreeningId = 2, Price = 8.50m }
            );
        }
    }
}
