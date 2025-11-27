using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class DailyPuzzleConfiguration : IEntityTypeConfiguration<DailyPuzzle>
{
    public void Configure(EntityTypeBuilder<DailyPuzzle> builder)
    {
        builder.HasKey(puzzle => puzzle.Id);

        builder.Property(puzzle => puzzle.PuzzleDate)
            .IsRequired();

        builder.Property(puzzle => puzzle.Solution)
            .HasMaxLength(5);

        builder.HasIndex(puzzle => puzzle.PuzzleDate)
            .IsUnique();

        builder.HasMany(puzzle => puzzle.Attempts)
            .WithOne(attempt => attempt.DailyPuzzle)
            .HasForeignKey(attempt => attempt.DailyPuzzleId);
    }
}
