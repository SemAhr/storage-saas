using Microsoft.EntityFrameworkCore;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;

namespace MediaService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.File> Media => Set<Domain.Entities.File>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Status>();

        var model = modelBuilder.Entity<Domain.Entities.File>();

        model.HasKey(file => file.Id);
        model.HasKey(file => file.NodeId);
        model.Property(file => file.Name).HasMaxLength(255).IsRequired();
        model.Property(file => file.MimeType).HasMaxLength(128).IsRequired();
        model.Property(file => file.StoragePath).HasMaxLength(2000).IsRequired();
        model.Property(file => file.Status).HasDefaultValue(Status.Pending);
        model.Property(file => file.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        model.Property(file => file.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        model.Property(file => file.DeletedAt).HasDefaultValue(null);
    }
}
