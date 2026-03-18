using MediaService.Contracts.File;
using MediaService.Contracts.Shared;
using MediaService.Data;

namespace MediaService.Application.File;

public sealed class FileService(AppDbContext dbContext) : IFileService
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default)
    {
        return new Task<PresignedResponseDto>(() => new PresignedResponseDto
        {
            NodeId = Guid.NewGuid(),
            UploadUrl = "https://example.com/upload",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Key = "example-file-key"
        });
    }

    public Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default)
    {
        return new Task<SuccessDto>(() => new SuccessDto
        {
            Message = "File upload confirmed successfully."
        });
    }
}
