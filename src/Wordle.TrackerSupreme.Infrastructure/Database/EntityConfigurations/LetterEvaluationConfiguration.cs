using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class LetterEvaluationConfiguration : IEntityTypeConfiguration<LetterEvaluation>
{
    public void Configure(EntityTypeBuilder<LetterEvaluation> builder)
    {
        builder.HasKey(evaluation => evaluation.Id);

        builder.Property(evaluation => evaluation.Letter)
            .IsRequired();

        builder.Property(evaluation => evaluation.Position)
            .IsRequired();

        builder.Property(evaluation => evaluation.Result)
            .IsRequired();

        builder.HasIndex(evaluation => new { evaluation.GuessAttemptId, evaluation.Position })
            .IsUnique();
    }
}
