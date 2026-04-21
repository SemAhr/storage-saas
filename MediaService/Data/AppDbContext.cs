using Microsoft.EntityFrameworkCore;
using MediaService.Domain.Entities;

namespace MediaService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NodeEntity> Nodes => Set<NodeEntity>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
}
