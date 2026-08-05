using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using CineVision.Model.Exceptions;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Services.Database;
using CineVision.Services.MovieStateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly CineVisionDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatBotService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IValidator<ChatRequest> _validator;

        public ChatBotService(
            CineVisionDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatBotService> logger,
            IValidator<ChatRequest> validator)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _validator = validator;
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request, string userRole)
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ClientException("OpenAI API key is not configured. Add your key to OpenAI:ApiKey in appsettings.json or user secrets.");
            }

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var maxTokens = int.TryParse(_configuration["OpenAI:MaxTokens"], out var mt) ? mt : 800;

            var contextSnapshot = await BuildCinemaContextAsync();
            var systemPrompt = BuildSystemPrompt(userRole, contextSnapshot);

            var messages = new List<OpenAiMessage>
            {
                new("system", systemPrompt)
            };

            if (request.History != null)
            {
                foreach (var turn in request.History.TakeLast(20))
                {
                    var role = turn.Role?.Trim().ToLowerInvariant();
                    if (role is "user" or "assistant" && !string.IsNullOrWhiteSpace(turn.Content))
                    {
                        messages.Add(new OpenAiMessage(role, turn.Content.Trim()));
                    }
                }
            }

            messages.Add(new OpenAiMessage("user", request.Message.Trim()));

            var payload = new OpenAiChatRequest
            {
                Model = model,
                MaxTokens = maxTokens,
                Messages = messages
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync("chat/completions", payload, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach OpenAI API.");
                throw new ClientException("Could not reach the OpenAI service. Check your network connection and try again.");
            }

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI API returned {Status}: {Body}", response.StatusCode, body);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new ClientException(
                        "OpenAI API key is invalid or expired. Update OpenAI__ApiKey in CineVision/.env and restart the API.");
                }
                throw new ClientException($"OpenAI request failed ({(int)response.StatusCode}). Verify your API key and model name.");
            }

            var completion = JsonSerializer.Deserialize<OpenAiChatResponse>(body, JsonOptions);
            var reply = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(reply))
            {
                throw new ClientException("OpenAI returned an empty response.");
            }

            return new ChatResponse
            {
                Reply = reply,
                RepliedAt = DateTime.UtcNow
            };
        }

        private static string BuildSystemPrompt(string userRole, string contextSnapshot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the CineVision Cinema Assistant — a helpful internal tool for cinema staff.");
            sb.AppendLine($"The current user role is: {userRole}.");
            sb.AppendLine();
            sb.AppendLine("Answer questions about:");
            sb.AppendLine("- How to use the CineVision desktop workflow (movies, halls, projections/projections, users, analytics).");
            sb.AppendLine("- What data is currently stored in the cinema system (use the LIVE DATA SNAPSHOT below).");
            sb.AppendLine("- Operational guidance: scheduling projections, hall maintenance, reservations, payments.");
            sb.AppendLine();
            sb.AppendLine("Rules:");
            sb.AppendLine("- Be concise, practical, and accurate. Use bullet points when listing items.");
            sb.AppendLine("- Base factual answers about current data ONLY on the snapshot below. If something is not in the snapshot, say you do not have that detail.");
            sb.AppendLine("- Never reveal password hashes, JWT secrets, or Stripe secret keys.");
            sb.AppendLine("- Do not invent movies, halls, or projections that are not in the snapshot.");
            sb.AppendLine("- Customers use the mobile app; staff/admins use the desktop app.");
            sb.AppendLine();
            sb.AppendLine("=== WORKFLOW CHEAT SHEET ===");
            sb.AppendLine("1. Movies: create in Draft → Activate when ready. Poster via PosterImageBase64 or PUT /Movies/{id}/Poster.");
            sb.AppendLine("2. Halls: create with RowsCount × SeatsPerRow to auto-generate seats. Screen types: Standard, IMAX, 3D. Status: Active, Maintenance, Inactive.");
            sb.AppendLine("3. Projections (Projections): pick an existing Movie + an Active Hall + date/time + price. Cannot schedule in Maintenance/Inactive halls.");
            sb.AppendLine("4. Reservations: customers reserve seats on mobile; payment via Stripe. Admin/Staff manage content; only Admin manages user accounts.");
            sb.AppendLine("5. Analytics: dashboard shows revenue (Paid reservations), tickets sold, occupancy, hall utilization.");
            sb.AppendLine("6. Email: admin can email users; reservation confirmations are queued via RabbitMQ when configured.");
            sb.AppendLine();
            sb.AppendLine("=== LIVE DATA SNAPSHOT (UTC) ===");
            sb.AppendLine(contextSnapshot);
            return sb.ToString();
        }

        private async Task<string> BuildCinemaContextAsync()
        {
            var now = DateTime.UtcNow;
            var horizon = now.AddDays(14);

            var movies = await _dbContext.Movies
                .AsNoTracking()
                .Include(m => m.Genre)
                .Include(m => m.AgeRating)
                .OrderBy(m => m.Title)
                .ToListAsync();

            var halls = await _dbContext.Halls
                .AsNoTracking()
                .Include(h => h.Seats)
                .Include(h => h.ScreenType)
                .Include(h => h.Status)
                .OrderBy(h => h.Name)
                .ToListAsync();

            var upcomingProjections = await _dbContext.Projections
                .AsNoTracking()
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.IsActive && s.StartTime >= now && s.StartTime <= horizon)
                .OrderBy(s => s.StartTime)
                .Take(25)
                .ToListAsync();

            var reservationStats = await _dbContext.Reservations
                .AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var ticketsSold = await _dbContext.ReservationSeats.CountAsync();

            var userCounts = await _dbContext.UserRoles
                .AsNoTracking()
                .Include(ur => ur.Role)
                .GroupBy(ur => ur.Role!.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var topMoviesByTickets = await _dbContext.ReservationSeats
                .AsNoTracking()
                .Include(rs => rs.Projection)
                .GroupBy(rs => rs.Projection!.MovieId)
                .Select(g => new { MovieId = g.Key, Tickets = g.Count() })
                .OrderByDescending(x => x.Tickets)
                .Take(5)
                .ToListAsync();

            var movieTitles = movies.ToDictionary(m => m.Id, m => m.Title);

            var avgRatings = await _dbContext.Reviews
                .AsNoTracking()
                .GroupBy(r => r.MovieId)
                .Select(g => new { MovieId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Generated at: {now:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();

            sb.AppendLine($"Movies: {movies.Count} total, {movies.Count(m => m.IsActive)} active, {movies.Count(m => m.MovieState == MovieLifecycleState.Active)} in Active state, {movies.Count(m => m.MovieState == MovieLifecycleState.Draft)} in Draft.");
            foreach (var m in movies.Where(m => m.IsActive).Take(12))
            {
                var genre = m.Genre?.Name ?? "—";
                var rating = avgRatings.FirstOrDefault(r => r.MovieId == m.Id);
                var ratingText = rating != null ? $"{rating.Avg:F1} ({rating.Count} reviews)" : "no reviews";
                sb.AppendLine($"  - {m.Title} | {genre} | {m.AgeRating?.Name ?? "—"} | {m.DurationMinutes} min | state={m.MovieState} | views={m.ViewCount} | avg rating={ratingText}");
            }
            if (movies.Count(m => m.IsActive) > 12)
            {
                sb.AppendLine($"  ... and {movies.Count(m => m.IsActive) - 12} more active movies");
            }

            sb.AppendLine();
            sb.AppendLine($"Halls: {halls.Count}");
            foreach (var h in halls)
            {
                var cap = h.Seats.Count(s => s.IsActive);
                sb.AppendLine($"  - {h.Name} | {cap} seats | {h.ScreenType?.Name ?? "—"} | status={h.Status?.Name ?? "—"} | active={h.IsActive}");
            }

            sb.AppendLine();
            sb.AppendLine($"Upcoming projections (next 14 days, max 25 shown): {upcomingProjections.Count}");
            foreach (var s in upcomingProjections)
            {
                sb.AppendLine($"  - {s.Movie?.Title} in {s.Hall?.Name} | {s.StartTime:yyyy-MM-dd HH:mm} UTC | ${s.BasePrice:F2}");
            }

            sb.AppendLine();
            sb.AppendLine($"Reservations by status: {string.Join(", ", reservationStats.Select(r => $"{r.Status}={r.Count}"))}");
            sb.AppendLine($"Total reserved seats (tickets): {ticketsSold}");

            sb.AppendLine();
            sb.AppendLine("Users by role: " + string.Join(", ", userCounts.Select(u => $"{u.Role}={u.Count}")));

            if (topMoviesByTickets.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Top movies by tickets sold:");
                foreach (var t in topMoviesByTickets)
                {
                    var title = movieTitles.TryGetValue(t.MovieId, out var name) ? name : $"Movie #{t.MovieId}";
                    sb.AppendLine($"  - {title}: {t.Tickets} tickets");
                }
            }

            return sb.ToString();
        }

        private sealed class OpenAiChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<OpenAiMessage> Messages { get; set; } = new();

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }
        }

        private sealed class OpenAiMessage
        {
            public OpenAiMessage() { }

            public OpenAiMessage(string role, string content)
            {
                Role = role;
                Content = content;
            }

            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class OpenAiChatResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenAiChoice>? Choices { get; set; }
        }

        private sealed class OpenAiChoice
        {
            [JsonPropertyName("message")]
            public OpenAiMessage? Message { get; set; }
        }
    }
}
