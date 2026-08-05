using Microsoft.EntityFrameworkCore;

namespace CineVision.Services.Database
{
    public partial class CineVisionDbContext : DbContext
    {
        public CineVisionDbContext(DbContextOptions<CineVisionDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Genre> Genres { get; set; }
        public DbSet<ScreenType> ScreenTypes { get; set; }
        public DbSet<HallStatus> HallStatuses { get; set; }
        public DbSet<AgeRating> AgeRatings { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Projection> Projections { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationSeat> ReservationSeats { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion<UtcDateTimeValueConverter>();

            configurationBuilder
                .Properties<DateTime?>()
                .HaveConversion<UtcNullableDateTimeValueConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            CreateConfiguration(modelBuilder);

            CreateSeed(modelBuilder);
        }
    }
}
