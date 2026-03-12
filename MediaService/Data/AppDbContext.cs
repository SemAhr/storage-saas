using Microsoft.EntityFrameworkCore;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;

namespace MediaService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Media> Media => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Status>();

        var model = modelBuilder.Entity<Media>();

        model.HasKey(media => media.Id);
        model.HasKey(media => media.NodeId);
        model.Property(media => media.Name).HasMaxLength(255).IsRequired();
        model.Property(media => media.MimeType).HasMaxLength(128).IsRequired();
        model.Property(media => media.StoragePath).HasMaxLength(2000).IsRequired();
        model.Property(media => media.Status).HasDefaultValue(Status.Pending);
        model.Property(media => media.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        model.Property(media => media.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        model.Property(media => media.DeletedAt).HasDefaultValue(null);
    }
}
