using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Models.Auth;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Domain.Repositories;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IPlayerRepository playerRepository,
    JwtTokenService tokenService,
    PasswordHasher<Player> passwordHasher,
    IWebHostEnvironment environment,
    ILogger<AuthController> logger)
    : ControllerBase
{
    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] SignUpRequest request, CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await playerRepository.IsDisplayNameTaken(displayName, null, cancellationToken))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = "Display name is already taken." });
        }

        if (await playerRepository.IsEmailTaken(email, null, cancellationToken))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = "Email is already registered." });
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            PasswordHash = string.Empty,
            IsAdmin = false
        };

        player.PasswordHash = passwordHasher.HashPassword(player, request.Password);

        await playerRepository.AddPlayer(player, cancellationToken);

        logger.LogInformation("Player registered. PlayerId={PlayerId} DisplayName={DisplayName}", player.Id, player.DisplayName);
        SignInPlayer(player);
        return Ok(new AuthResponse(MapPlayer(player), null));
    }

    [EnableRateLimiting(AuthRateLimiting.PolicyName)]
    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var player = await playerRepository.GetPlayerByEmail(email, cancellationToken);

        if (player is null)
        {
            logger.LogWarning("Sign-in failed: email not found. Email={Email}", email);
            return Unauthorized(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Detail = "Invalid credentials." });
        }

        var verification = passwordHasher.VerifyHashedPassword(player, player.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Sign-in failed: wrong password. PlayerId={PlayerId}", player.Id);
            return Unauthorized(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Detail = "Invalid credentials." });
        }

        logger.LogInformation("Player signed in. PlayerId={PlayerId}", player.Id);
        SignInPlayer(player);
        return Ok(new AuthResponse(MapPlayer(player), null));
    }

    [HttpPost("signout")]
    public IActionResult SignOutCurrentSession()
    {
        Response.Cookies.Delete(AuthCookie.CookieName, AuthCookie.BuildOptions(!environment.IsDevelopment()));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PlayerResponse>> GetCurrentPlayer(CancellationToken cancellationToken)
    {
        var playerIdClaim = User.FindFirstValue("playerId");
        if (!Guid.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized();
        }

        var player = await playerRepository.GetPlayer(playerId, cancellationToken);
        if (player is null)
        {
            return Unauthorized();
        }

        return Ok(MapPlayer(player));
    }

    private static PlayerResponse MapPlayer(Player player)
        => new(player.Id, player.DisplayName, player.Email, player.CreatedOn, player.IsAdmin);

    private void SignInPlayer(Player player)
    {
        var token = tokenService.CreateToken(player);
        Response.Cookies.Append(
            AuthCookie.CookieName,
            token,
            AuthCookie.BuildOptions(!environment.IsDevelopment()));
    }
}
