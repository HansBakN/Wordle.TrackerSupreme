using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Application.Services.Admin;
using Wordle.TrackerSupreme.Application.Services.Game;
using Wordle.TrackerSupreme.Application.Services;
using Wordle.TrackerSupreme.Domain.Services;
using Wordle.TrackerSupreme.Domain.Services.Game;
using Wordle.TrackerSupreme.Infrastructure;
using Wordle.TrackerSupreme.Infrastructure.Database;
using Wordle.TrackerSupreme.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Wordle Tracker Supreme API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddWordleTrackerSupremeInfrastructure(builder.Configuration, configureNpgsql: npgsql => npgsql.AllowMigrationManagement());
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireClaim("isAdmin", "true");
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
        var secret = jwtSection.GetValue<string>("Secret") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is missing. Set Jwt:Secret in configuration.");
        }

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        if (secretBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 bytes for HS256. Provide a longer Jwt:Secret.");
        }

        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<GameOptions>>().Value);
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<PasswordHasher<Wordle.TrackerSupreme.Domain.Models.Player>>();
builder.Services.AddSingleton<IGameClock, GameClock>();
builder.Services.AddSingleton<IWordSelector, WordSelector>();
builder.Services.AddSingleton<WordListValidator>();
builder.Services.AddSingleton<IWordValidator>(sp => sp.GetRequiredService<WordListValidator>());
builder.Services.AddSingleton<IWordListProvider>(sp => sp.GetRequiredService<WordListValidator>());
builder.Services.AddSingleton<IGuessEvaluationService, GuessEvaluationService>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IOfficialWordProvider, DevelopmentOfficialWordProvider>();
}
else
{
    builder.Services.AddHttpClient<OfficialWordProvider>(client =>
    {
        client.BaseAddress = new Uri("https://www.nytimes.com/");
    });
    builder.Services.AddScoped<IOfficialWordProvider>(sp => sp.GetRequiredService<OfficialWordProvider>());
}
builder.Services.AddScoped<IGameplayService, GameplayService>();
builder.Services.AddScoped<IDailyPuzzleService, DailyPuzzleService>();
builder.Services.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
    ["http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173", "http://127.0.0.1:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (WordleTrackerSupremeDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return Results.StatusCode(503);
        }

        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

app.MapControllers();

app.Run();
