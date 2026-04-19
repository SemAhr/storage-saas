using Microsoft.EntityFrameworkCore;
using MediaService.Domain.Entities;

namespace MediaService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<FileEntry> Files => Set<FileEntry>();
}
