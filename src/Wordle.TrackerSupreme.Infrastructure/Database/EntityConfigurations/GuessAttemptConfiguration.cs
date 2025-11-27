using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class GuessAttemptConfiguration : IEntityTypeConfiguration<GuessAttempt>
{
    public void Configure(EntityTypeBuilder<GuessAttempt> builder)
    {
        builder.HasKey(guess => guess.Id);

        builder.Property(guess => guess.GuessWord)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasIndex(guess => new { guess.PlayerPuzzleAttemptId, guess.GuessNumber })
            .IsUnique();

        builder.HasMany(guess => guess.Feedback)
            .WithOne(feedback => feedback.GuessAttempt)
            .HasForeignKey(feedback => feedback.GuessAttemptId);
    }
}
