using CineVision.Common.Services.CryptoService;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.Access;
using CineVision.Services;
using CineVision.Services.Database;
using CineVision.Services.Validators;
using CineVision.WebAPI.Filters;
using CineVision.WebAPI.Hubs;
using CineVision.WebAPI.Serialization;
using CineVision.WebAPI.Services;
using CineVision.WebAPI.Services.AccessManager;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;

// Load CineVision/.env before CreateBuilder so env vars override empty appsettings secrets.
// Docker Compose already injects env; EnvFileLoader will not overwrite existing variables.
EnvFileLoader.Load(
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthenticatedUserAccessor, HttpAuthenticatedUserAccessor>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
    options.ModelBinderProviders.Insert(0, new UtcDateTimeModelBinderProvider());
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    // Enums travel over the wire as their names (e.g. ReservationStatus.Paid → "Paid").
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add Entity Framework Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CineVisionDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// register Mapster for object mapping
builder.Services.AddMapster();

// Mapster configuration for the cinema domain.
TypeAdapterConfig<Genre, GenreResponse>.NewConfig().IgnoreNullValues(true);

// Reference (lookup) tables. Navigation collections must never be mapped onto responses.
TypeAdapterConfig<ScreenType, ScreenTypeResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<HallStatus, HallStatusResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<AgeRating, AgeRatingResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Language, LanguageResponse>.NewConfig().IgnoreNullValues(true);

// Lookup names are flattened into the movie/projection/hall responses so clients can render
// a label without fetching the reference tables.
TypeAdapterConfig<Movie, MovieResponse>.NewConfig()
    .IgnoreNullValues(true)
    .Map(dest => dest.Language, src => src.Language != null ? src.Language.Name : null)
    .Map(dest => dest.AgeRating, src => src.AgeRating != null ? src.AgeRating.Name : null);
TypeAdapterConfig<MovieUpdateRequest, Movie>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Hall, HallResponse>.NewConfig()
    .IgnoreNullValues(true)
    .Map(dest => dest.ScreenTypeName, src => src.ScreenType != null ? src.ScreenType.Name : string.Empty)
    .Map(dest => dest.StatusName, src => src.Status != null ? src.Status.Name : string.Empty)
    .Map(dest => dest.AllowsProjections, src => src.Status != null && src.Status.AllowsProjections);
TypeAdapterConfig<Seat, SeatResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Projection, ProjectionResponse>.NewConfig()
    .IgnoreNullValues(true)
    .Map(dest => dest.Language, src => src.Language != null ? src.Language.Name : null)
    .Ignore(dest => dest.Movie)
    .Ignore(dest => dest.Hall);
TypeAdapterConfig<User, UserResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UserUpdateRequest, User>.NewConfig().IgnoreNullValues(true);

// register application services
builder.Services.AddScoped<IMovieService, MovieService>();

// cinema domain services
builder.Services.AddScoped<IGenreService, GenreService>();

// reference (lookup) data services
builder.Services.AddScoped<IScreenTypeService, ScreenTypeService>();
builder.Services.AddScoped<IHallStatusService, HallStatusService>();
builder.Services.AddScoped<IAgeRatingService, AgeRatingService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();

builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAnalyticsRealtimePublisher, AnalyticsRealtimePublisher>();
builder.Services.AddScoped<IAnalyticsNotifier, AnalyticsNotifier>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(90);
});

// shared services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationPushNotifier, NotificationPushNotifier>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<ITokenRevocationService, TokenRevocationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAccessManager, AccessManager>();
builder.Services.AddScoped<ICryptoService, CryptoService>();

// email: API only publishes to RabbitMQ; CineVision.Worker container consumes and sends SMTP
var rabbitMqEnabled = builder.Configuration.GetValue("RabbitMq:Enabled", false);
if (rabbitMqEnabled)
{
    builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
    builder.Services.AddScoped<IEmailService, RabbitMqEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();
}

// validators
builder.Services.AddScoped<IValidator<GenreInsertRequest>, GenreInsertValidator>();
builder.Services.AddScoped<IValidator<GenreUpdateRequest>, GenreUpdateValidator>();

// reference data: screen types and hall statuses only need the shared name/description rules
builder.Services.AddScoped<IValidator<ScreenTypeInsertRequest>, LookupRequestValidator<ScreenTypeInsertRequest>>();
builder.Services.AddScoped<IValidator<ScreenTypeUpdateRequest>, LookupRequestValidator<ScreenTypeUpdateRequest>>();
builder.Services.AddScoped<IValidator<HallStatusInsertRequest>, LookupRequestValidator<HallStatusInsertRequest>>();
builder.Services.AddScoped<IValidator<HallStatusUpdateRequest>, LookupRequestValidator<HallStatusUpdateRequest>>();
builder.Services.AddScoped<IValidator<AgeRatingInsertRequest>, AgeRatingInsertValidator>();
builder.Services.AddScoped<IValidator<AgeRatingUpdateRequest>, AgeRatingUpdateValidator>();
builder.Services.AddScoped<IValidator<LanguageInsertRequest>, LanguageInsertValidator>();
builder.Services.AddScoped<IValidator<LanguageUpdateRequest>, LanguageUpdateValidator>();

builder.Services.AddScoped<IValidator<NewsInsertRequest>, NewsInsertValidator>();
builder.Services.AddScoped<IValidator<NewsUpdateRequest>, NewsUpdateValidator>();
builder.Services.AddScoped<IValidator<MovieInsertRequest>, MovieInsertValidator>();
builder.Services.AddScoped<IValidator<MovieUpdateRequest>, MovieUpdateValidator>();
builder.Services.AddScoped<IValidator<HallInsertRequest>, HallInsertValidator>();
builder.Services.AddScoped<IValidator<HallUpdateRequest>, HallUpdateValidator>();
builder.Services.AddScoped<IValidator<SeatInsertRequest>, SeatInsertValidator>();
builder.Services.AddScoped<IValidator<SeatUpdateRequest>, SeatUpdateValidator>();
builder.Services.AddScoped<IValidator<ProjectionInsertRequest>, ProjectionInsertValidator>();
builder.Services.AddScoped<IValidator<ProjectionUpdateRequest>, ProjectionUpdateValidator>();
builder.Services.AddScoped<IValidator<UserInsertRequest>, UserInsertValidator>();
builder.Services.AddScoped<IValidator<UserRegisterRequest>, UserRegisterValidator>();
builder.Services.AddScoped<IValidator<UserUpdateRequest>, UserUpdateValidator>();
builder.Services.AddScoped<IValidator<UserProfileUpdateRequest>, UserProfileUpdateValidator>();
builder.Services.AddScoped<IValidator<ForgotPasswordRequest>, ForgotPasswordValidator>();
builder.Services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordValidator>();
builder.Services.AddScoped<IValidator<ReviewInsertRequest>, ReviewInsertValidator>();
builder.Services.AddScoped<IValidator<ReviewUpdateRequest>, ReviewUpdateValidator>();
builder.Services.AddScoped<IValidator<ChatRequest>, ChatRequestValidator>();
builder.Services.AddScoped<IValidator<ReservationCreateRequest>, ReservationCreateValidator>();
builder.Services.AddScoped<IValidator<CreatePaymentIntentRequest>, CreatePaymentIntentValidator>();
builder.Services.AddScoped<IValidator<ReservationCancelRequest>, ReservationCancelValidator>();
builder.Services.AddScoped<IValidator<UserPasswordChangeRequest>, UserPasswordChangeValidator>();
builder.Services.AddScoped<IValidator<UserLoginRequest>, UserLoginValidator>();
builder.Services.AddScoped<IValidator<RefreshAccessTokenRequest>, RefreshAccessTokenValidator>();
builder.Services.AddScoped<IValidator<HallSeatLayoutUpdateRequest>, HallSeatLayoutUpdateValidator>();
builder.Services.AddScoped<IValidator<MoviePosterUpdateRequest>, MoviePosterUpdateValidator>();
builder.Services.AddScoped<IValidator<EmailSendRequest>, EmailSendValidator>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    // Keep our custom claim names ("Id", "Role") instead of remapping them to long URIs.
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JwtToken:Issuer"],
        ValidAudience = builder.Configuration["JwtToken:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtToken:SecretKey"] ?? string.Empty)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        // Tell ASP.NET which JWT claims carry the user name and role so that
        // User.IsInRole(...) and [Authorize(Roles = RoleNames.Admin)] work off the token.
        NameClaimType = ClaimNames.Id,
        RoleClaimType = ClaimNames.Role
    };
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        // Reject JWTs whose token version no longer matches (logout / disabled user).
        OnTokenValidated = async context =>
        {
            var claims = context.Principal;
            if (!int.TryParse(claims?.FindFirst(ClaimNames.Id)?.Value, out var userId) ||
                !int.TryParse(claims?.FindFirst(ClaimNames.TokenVersion)?.Value, out var tokenVersion))
            {
                context.Fail("Token is missing the identity claims.");
                return;
            }

            var revocation = context.HttpContext.RequestServices
                .GetRequiredService<ITokenRevocationService>();

            if (!await revocation.IsAccessTokenValidAsync(userId, tokenVersion))
            {
                context.Fail("This session has ended. Please sign in again.");
            }
        }
    };
});
builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "CineVision API",
            Description = "API for managing movies, halls, projections and seat reservations"
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            BearerFormat = "JWT",
            Name = "JWT Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("CineVisionCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5126",
                "http://127.0.0.1:5126",
                "http://localhost:3000",
                "http://localhost:8080",
                "http://localhost:5000",
                "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Persist JWT from Login in localStorage so Swagger sends it automatically.
        options.UseRequestInterceptor(
            "(request) => {" +
            "  const token = window.localStorage.getItem('cinevision_token');" +
            "  if (token && !request.headers['Authorization']) { request.headers['Authorization'] = 'Bearer ' + token; }" +
            "  return request;" +
            "}");
        options.UseResponseInterceptor(
            "(response) => {" +
            "  try {" +
            "    if (response.url && response.url.indexOf('/Access/Login') !== -1 && response.status === 200) {" +
            "      const data = JSON.parse(response.text);" +
            "      const token = data.accesstoken || data.accessToken || data.Accesstoken;" +
            "      if (token) { window.localStorage.setItem('cinevision_token', token); }" +
            "    }" +
            "    if (response.url && response.url.indexOf('/Access/Logout') !== -1 && response.status === 200) {" +
            "      window.localStorage.removeItem('cinevision_token');" +
            "    }" +
            "  } catch (e) {}" +
            "  return response;" +
            "}");
    });
}

app.UseCors("CineVisionCors");

app.UseAuthentication();

app.UseAuthorization();

// Liveness probe for Docker — no secrets; kept outside IsDevelopment on purpose.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();
app.MapHub<AnalyticsHub>("/hubs/analytics");
app.MapHub<NotificationsHub>("/hubs/notifications");

await EnsureDatabaseReadyAsync(app);

app.Run();

static async Task EnsureDatabaseReadyAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CineVisionDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    const int maxAttempts = 40;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await MoviePosterSeed.EnsureSeededAsync(db);
            await NewsImageSeed.EnsureSeededAsync(db);
            logger.LogInformation("Database migrated; poster and news image seed ensured.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "Database not ready (attempt {Attempt}/{Max}). Retrying in 3s...",
                attempt,
                maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
