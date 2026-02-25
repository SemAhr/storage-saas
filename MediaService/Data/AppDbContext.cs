using Microsoft.EntityFrameworkCore;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;

namespace MediaService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Media> Media => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Status>();

        var model = modelBuilder.Entity<Media>();

        model.HasKey(media => media.Id);

        model.Property(media => media.FileName).HasMaxLength(255).IsRequired();
        model.Property(media => media.MimeType).HasMaxLength(128).IsRequired();
        model.Property(media => media.Url).HasMaxLength(2000).IsRequired();
        model.Property(media => media.Status).IsRequired(); // HasDefaultValue(Status.Pending);
        model.Property(media => media.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        model.Property(media => media.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
    }
}
