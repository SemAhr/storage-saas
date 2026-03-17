using MediaService.Contracts.Media;
using MediaService.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Application.Media;

public sealed class MediaService(AppDbContext dbContext) : IMediaService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<FileDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Media
            .AsNoTracking()
            .Where(item => item.DeletedAt == null)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. items.Select(item => new FileDto(
            item.Id,
            item.NodeId,
            item.Name,
            item.MimeType,
            item.Size,
            item.StoragePath,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt,
            item.DeletedAt
        ))];
    }
}
