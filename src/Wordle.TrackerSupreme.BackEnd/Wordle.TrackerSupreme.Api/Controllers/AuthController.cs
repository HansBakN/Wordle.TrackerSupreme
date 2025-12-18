using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Api.Auth;
using Wordle.TrackerSupreme.Api.Models.Auth;
using Wordle.TrackerSupreme.Domain.Models;
using Wordle.TrackerSupreme.Infrastructure.Database;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    WordleTrackerSupremeDbContext dbContext,
    JwtTokenService tokenService,
    PasswordHasher<Player> passwordHasher)
    : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] SignUpRequest request)
    {
        var displayName = request.DisplayName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await dbContext.Players.AnyAsync(p => p.DisplayName == displayName);
        if (exists)
        {
            return Conflict(new { message = "Display name is already taken." });
        }

        var emailExists = await dbContext.Players.AnyAsync(p => p.Email == email);
        if (emailExists)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            PasswordHash = string.Empty
        };

        player.PasswordHash = passwordHasher.HashPassword(player, request.Password);

        dbContext.Players.Add(player);
        await dbContext.SaveChangesAsync();

        var token = tokenService.CreateToken(player);
        return Ok(new AuthResponse(MapPlayer(player), token));
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Email == email);

        if (player is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var verification = passwordHasher.VerifyHashedPassword(player, player.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = tokenService.CreateToken(player);
        return Ok(new AuthResponse(MapPlayer(player), token));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PlayerResponse>> GetCurrentPlayer()
    {
        var playerIdClaim = User.FindFirstValue("playerId");
        if (!Guid.TryParse(playerIdClaim, out var playerId))
        {
            return Unauthorized();
        }

        var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);
        if (player is null)
        {
            return Unauthorized();
        }

        return Ok(MapPlayer(player));
    }

    private static PlayerResponse MapPlayer(Player player)
        => new(player.Id, player.DisplayName, player.Email, player.CreatedOn);
}
