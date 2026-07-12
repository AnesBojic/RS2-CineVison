using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ECommerceDbContext _dbContext;

        public NotificationService(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dbContext.UserNotifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<List<NotificationResponse>> GetForUserAsync(int userId, int limit = 50)
        {
            return await _dbContext.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<NotificationResponse> CreateAsync(int userId, string title, string message, string type)
        {
            var entity = new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserNotifications.Add(entity);
            await _dbContext.SaveChangesAsync();

            return new NotificationResponse
            {
                Id = entity.Id,
                Title = entity.Title,
                Message = entity.Message,
                Type = entity.Type,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task MarkAsReadAsync(int userId, int notificationId)
        {
            var entity = await _dbContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Notification {notificationId} not found.");
            }

            entity.IsRead = true;
            await _dbContext.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync(int userId, string? type = null)
        {
            var query = _dbContext.UserNotifications.Where(n => n.UserId == userId && !n.IsRead);

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(n => n.Type == type);
            }

            var items = await query.ToListAsync();
            foreach (var item in items)
            {
                item.IsRead = true;
            }

            if (items.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
