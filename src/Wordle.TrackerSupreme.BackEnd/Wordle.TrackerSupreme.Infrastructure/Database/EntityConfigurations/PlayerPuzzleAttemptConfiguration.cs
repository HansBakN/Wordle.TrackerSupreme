using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class PlayerPuzzleAttemptConfiguration : IEntityTypeConfiguration<PlayerPuzzleAttempt>
{
    public void Configure(EntityTypeBuilder<PlayerPuzzleAttempt> builder)
    {
        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.Status)
            .HasDefaultValue(AttemptStatus.InProgress);

        builder.Property(attempt => attempt.CreatedOn)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.HasIndex(attempt => new { attempt.PlayerId, attempt.DailyPuzzleId })
            .IsUnique();

        builder.HasMany(attempt => attempt.Guesses)
            .WithOne(guess => guess.PlayerPuzzleAttempt)
            .HasForeignKey(guess => guess.PlayerPuzzleAttemptId);
    }
}
