using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Controllers;
using Wordle.TrackerSupreme.Api.Models.Auth;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;
using Wordle.TrackerSupreme.Tests.Fakes;
using Xunit;

namespace Wordle.TrackerSupreme.Tests;

public class AuthControllerTests
{
    // Test-only placeholder values — not real credentials
    private const string TestJwtSigningKey = "test-signing-key-at-least-32-chars-long";
    private const string TestCredential = "Password1";

    private static AuthController CreateController(
        IPlayerRepository repository,
        ClaimsPrincipal? user = null)
    {
        var signingKey = TestJwtSigningKey;
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = signingKey,
            Issuer = "test",
            Audience = "test",
            ExpiryMinutes = 60
        });

        var env = new FakeWebHostEnvironment();

        var controller = new AuthController(
            repository,
            new JwtTokenService(jwtSettings),
            new PasswordHasher<Player>(),
            env,
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal()
                }
            }
        };

        return controller;
    }

    [Fact]
    public async Task SignUp_creates_player_and_returns_ok_with_token()
    {
        var repo = new FakeAuthRepository();
        var controller = CreateController(repo);

        var result = await controller.SignUp(
            new SignUpRequest { DisplayName = "Alice", Email = "alice@example.com", Password = TestCredential },
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        repo.Players.Should().ContainSingle(p => p.Email == "alice@example.com");
    }

    [Fact]
    public async Task SignUp_returns_conflict_when_display_name_is_taken()
    {
        var existing = new Player
        {
            Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = ""
        };
        var repo = new FakeAuthRepository([existing]);
        var controller = CreateController(repo);

        var result = await controller.SignUp(
            new SignUpRequest { DisplayName = "Alice", Email = "new@example.com", Password = TestCredential },
            CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SignUp_returns_conflict_when_email_is_taken()
    {
        var existing = new Player
        {
            Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = ""
        };
        var repo = new FakeAuthRepository([existing]);
        var controller = CreateController(repo);

        var result = await controller.SignUp(
            new SignUpRequest { DisplayName = "Bob", Email = "alice@example.com", Password = TestCredential },
            CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SignIn_returns_ok_with_token_for_valid_credentials()
    {
        var hasher = new PasswordHasher<Player>();
        var player = new Player { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = "" };
        player.PasswordHash = hasher.HashPassword(player, TestCredential);
        var repo = new FakeAuthRepository([player]);
        var controller = CreateController(repo);

        var result = await controller.SignIn(
            new SignInRequest { Email = "alice@example.com", Password = TestCredential },
            CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SignIn_returns_unauthorized_for_wrong_password()
    {
        var hasher = new PasswordHasher<Player>();
        var player = new Player { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = "" };
        player.PasswordHash = hasher.HashPassword(player, TestCredential);
        var repo = new FakeAuthRepository([player]);
        var controller = CreateController(repo);

        var result = await controller.SignIn(
            new SignInRequest { Email = "alice@example.com", Password = TestCredential + "_wrong" },
            CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SignIn_returns_unauthorized_for_unknown_email()
    {
        var repo = new FakeAuthRepository();
        var controller = CreateController(repo);

        var result = await controller.SignIn(
            new SignInRequest { Email = "nobody@example.com", Password = TestCredential },
            CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetCurrentPlayer_returns_player_for_valid_jwt_claim()
    {
        var player = new Player { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "alice@example.com", PasswordHash = "" };
        var repo = new FakeAuthRepository([player]);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("playerId", player.Id.ToString())], "Test"));
        var controller = CreateController(repo, user);

        var result = await controller.GetCurrentPlayer(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        var response = ok.Value.Should().BeOfType<PlayerResponse>().Which;
        response.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetCurrentPlayer_returns_unauthorized_when_player_not_found()
    {
        var repo = new FakeAuthRepository();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("playerId", Guid.NewGuid().ToString())], "Test"));
        var controller = CreateController(repo, user);

        var result = await controller.GetCurrentPlayer(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    private sealed class FakeAuthRepository : IPlayerRepository
    {
        public List<Player> Players { get; }

        public FakeAuthRepository(List<Player>? players = null) => Players = players ?? [];

        public Task<Player?> GetPlayer(Guid playerId, CancellationToken ct)
            => Task.FromResult(Players.FirstOrDefault(p => p.Id == playerId));

        public Task<Player?> GetPlayerByEmail(string email, CancellationToken ct)
            => Task.FromResult(Players.FirstOrDefault(p => p.Email == email));

        public Task<Player?> GetPlayerWithAttempts(Guid playerId, CancellationToken ct)
            => Task.FromResult(Players.FirstOrDefault(p => p.Id == playerId));

        public Task<Player?> GetPlayerWithAttemptsAndGuesses(Guid playerId, CancellationToken ct)
            => Task.FromResult(Players.FirstOrDefault(p => p.Id == playerId));

        public Task<List<Player>> GetPlayers(CancellationToken ct) => Task.FromResult(Players.ToList());

        public Task<List<Player>> GetPlayersWithAttempts(CancellationToken ct) => Task.FromResult(Players.ToList());

        public Task AddPlayer(Player player, CancellationToken ct)
        {
            Players.Add(player);
            return Task.CompletedTask;
        }

        public Task<bool> IsDisplayNameTaken(string displayName, Guid? excludePlayerId, CancellationToken ct)
            => Task.FromResult(Players.Any(p =>
                p.DisplayName == displayName && (excludePlayerId == null || p.Id != excludePlayerId)));

        public Task<bool> IsEmailTaken(string email, Guid? excludePlayerId, CancellationToken ct)
            => Task.FromResult(Players.Any(p =>
                p.Email == email && (excludePlayerId == null || p.Id != excludePlayerId)));

        public Task SaveChanges(CancellationToken ct) => Task.CompletedTask;
    }
}
