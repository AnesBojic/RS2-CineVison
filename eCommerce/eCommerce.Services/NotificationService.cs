using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eCommerce.Services.Enums;

namespace eCommerce.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly INotificationPushNotifier _pushNotifier;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ECommerceDbContext dbContext,
            INotificationPushNotifier pushNotifier,
            ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _pushNotifier = pushNotifier;
            _logger = logger;
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
                    Type = n.Type.ToString(),
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<NotificationResponse> CreateAsync(int userId, string title, string message, NotificationType type)
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

            var response = new NotificationResponse
            {
                Id = entity.Id,
                Title = entity.Title,
                Message = entity.Message,
                Type = entity.Type.ToString(),
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };

            await PushSafeAsync(userId, response);
            return response;
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

            await PushUnreadCountSafeAsync(userId);
        }

        public async Task MarkAllReadAsync(int userId, NotificationType? type = null)
        {
            var query = _dbContext.UserNotifications.Where(n => n.UserId == userId && !n.IsRead);

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
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

            await PushUnreadCountSafeAsync(userId);
        }

        private async Task PushSafeAsync(int userId, NotificationResponse notification)
        {
            try
            {
                var unread = await GetUnreadCountAsync(userId);
                await _pushNotifier.NotifyUserAsync(userId, notification, unread);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push notification {NotificationId} to user {UserId}.", notification.Id, userId);
            }
        }

        private async Task PushUnreadCountSafeAsync(int userId)
        {
            try
            {
                var unread = await GetUnreadCountAsync(userId);
                await _pushNotifier.NotifyUnreadCountAsync(userId, unread);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push unread count to user {UserId}.", userId);
            }
        }
    }
}
