using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.MovieStateMachine;
using eCommerce.Services.Validators;
using eCommerce.WebAPI.Filters;
using eCommerce.WebAPI.Services;
using eCommerce.WebAPI.Services.AccessManager;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthenticatedUserAccessor, HttpAuthenticatedUserAccessor>();

builder.Services.AddControllers(
   options => options.Filters.Add<ExceptionFilter>()
);

// Add Entity Framework Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// register Mapster for object mapping
builder.Services.AddMapster();

// Mapster configuration for the cinema domain.
TypeAdapterConfig<Genre, GenreResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Movie, MovieResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<MovieUpdateRequest, Movie>.NewConfig().IgnoreNullValues(true).Ignore(dest => dest.Assets);
TypeAdapterConfig<Hall, HallResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Seat, SeatResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<Screening, ScreeningResponse>.NewConfig()
    .IgnoreNullValues(true)
    .Ignore(dest => dest.Movie)
    .Ignore(dest => dest.Hall);
TypeAdapterConfig<Asset, AssetResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<User, UserResponse>.NewConfig().IgnoreNullValues(true);
TypeAdapterConfig<UserUpdateRequest, User>.NewConfig().IgnoreNullValues(true);

// register application services
// movie service + state machine
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<BaseMovieState>();
builder.Services.AddScoped<InitialMovieState>();
builder.Services.AddScoped<DraftMovieState>();
builder.Services.AddScoped<ActiveMovieState>();

// cinema domain services
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IHallService, HallService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IScreeningService, ScreeningService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(90);
});

// shared services
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAccessManager, AccessManager>();
builder.Services.AddScoped<ICryptoService, CryptoService>();

// email: RabbitMQ producer + background consumer (optional — disable in Development when RabbitMQ is not running)
var rabbitMqEnabled = builder.Configuration.GetValue("RabbitMq:Enabled", false);
if (rabbitMqEnabled)
{
    builder.Services.AddScoped<IEmailService, RabbitMqEmailService>();
    builder.Services.AddHostedService<EmailConsumerBackgroundService>();
}
else
{
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();
}

// validators
builder.Services.AddScoped<IValidator<GenreInsertRequest>, GenreInsertValidator>();
builder.Services.AddScoped<IValidator<GenreUpdateRequest>, GenreUpdateValidator>();
builder.Services.AddScoped<IValidator<MovieInsertRequest>, MovieInsertValidator>();
builder.Services.AddScoped<IValidator<MovieUpdateRequest>, MovieUpdateValidator>();
builder.Services.AddScoped<IValidator<HallInsertRequest>, HallInsertValidator>();
builder.Services.AddScoped<IValidator<HallUpdateRequest>, HallUpdateValidator>();
builder.Services.AddScoped<IValidator<SeatInsertRequest>, SeatInsertValidator>();
builder.Services.AddScoped<IValidator<SeatUpdateRequest>, SeatUpdateValidator>();
builder.Services.AddScoped<IValidator<ScreeningInsertRequest>, ScreeningInsertValidator>();
builder.Services.AddScoped<IValidator<ScreeningUpdateRequest>, ScreeningUpdateValidator>();
builder.Services.AddScoped<IValidator<AssetInsertRequest>, AssetInsertValidator>();
builder.Services.AddScoped<IValidator<AssetUpdateRequest>, AssetUpdateValidator>();
builder.Services.AddScoped<IValidator<UserInsertRequest>, UserInsertValidator>();
builder.Services.AddScoped<IValidator<UserUpdateRequest>, UserUpdateValidator>();
builder.Services.AddScoped<IValidator<UserProfileUpdateRequest>, UserProfileUpdateValidator>();
builder.Services.AddScoped<IValidator<ReviewInsertRequest>, ReviewInsertValidator>();
builder.Services.AddScoped<IValidator<ReviewUpdateRequest>, ReviewUpdateValidator>();
builder.Services.AddScoped<IValidator<ChatRequest>, ChatRequestValidator>();

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
        // User.IsInRole(...) and [Authorize(Roles = "Admin")] work off the token.
        NameClaimType = "Id",
        RoleClaimType = "Role"
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
            Description = "API for managing movies, halls, screenings and seat reservations"
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

var app = builder.Build();

// Configure the HTTP request pipeline.
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Automatically capture the JWT returned by POST /Access/Login and attach it as a
        // Bearer header on every subsequent request, so there is no need to use the
        // "Authorize" button or copy/paste tokens. Logging out clears the stored token.
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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
    await MoviePosterSeed.EnsureSeededAsync(db);
}

app.Run();
