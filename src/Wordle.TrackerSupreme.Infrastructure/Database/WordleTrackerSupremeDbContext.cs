using Microsoft.EntityFrameworkCore;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database;

public class WordleTrackerSupremeDbContext(DbContextOptions<WordleTrackerSupremeDbContext> options)
    : DbContext(options)
{
    public DbSet<Player> Players { get; set; }
    public DbSet<DailyPuzzle> DailyPuzzles { get; set; }
    public DbSet<PlayerPuzzleAttempt> Attempts { get; set; }
    public DbSet<GuessAttempt> Guesses { get; set; }
    public DbSet<LetterEvaluation> LetterEvaluations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WordleTrackerSupremeDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
