using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordle.TrackerSupreme.Domain.Models;

namespace Wordle.TrackerSupreme.Infrastructure.Database.EntityConfigurations;

public class NytImportSessionConfiguration : IEntityTypeConfiguration<NytImportSession>
{
    public void Configure(EntityTypeBuilder<NytImportSession> builder)
    {
        builder.HasKey(session => session.Id);
        builder.Property(session => session.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(session => session.CodeHash).IsUnique();
        builder.HasIndex(session => new { session.PlayerId, session.CreatedOn });
        builder.HasOne(session => session.Player).WithMany().HasForeignKey(session => session.PlayerId).OnDelete(DeleteBehavior.Cascade);
    }
}
