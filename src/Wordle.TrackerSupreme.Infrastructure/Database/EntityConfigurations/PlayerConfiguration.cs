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

        builder.Property(player => player.CreatedOn)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.HasMany(player => player.Attempts)
            .WithOne(attempt => attempt.Player)
            .HasForeignKey(attempt => attempt.PlayerId);
    }
}
