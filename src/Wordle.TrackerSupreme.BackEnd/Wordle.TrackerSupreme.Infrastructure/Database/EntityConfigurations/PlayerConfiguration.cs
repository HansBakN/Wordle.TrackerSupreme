using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(player => player.Id);

        builder.Property(player => player.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(player => player.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(player => player.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(player => player.IsAdmin)
            .HasDefaultValue(false);

        builder.Property(player => player.CreatedOn)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.HasIndex(player => player.DisplayName)
            .IsUnique();

        builder.HasIndex(player => player.Email)
            .IsUnique();

        builder.HasMany(player => player.Attempts)
            .WithOne(attempt => attempt.Player)
            .HasForeignKey(attempt => attempt.PlayerId);
    }
}
