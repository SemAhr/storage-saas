using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NodeEntity> Nodes => Set<NodeEntity>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
}
