using MediaService.Application.Storage;
using MediaService.Domain.Exceptions;
using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Shared;

namespace MediaService.Application.Files;

public sealed class FileService(IFileRepository fileRepository, IStorageService storageService) : IFileService
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;

    public async Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = new FileEntity
            {
                MimeType = presignedUploadDto.MimeType,
                Size = presignedUploadDto.Size,
                Node = new NodeEntity
                {
                    ParentId = presignedUploadDto.ParentId,
                    Name = presignedUploadDto.FileName,
                    Type = NodeType.File
                }
            };

            // Generate a presigned URL for the file upload using the storage service and return it to the client
            var key = _storageService.GenerateS3Key(presignedUploadDto.FileName, file.NodeId);
            var uploaded = _storageService.GenerateUploadUrl(key, presignedUploadDto.MimeType);

            return new PresignedResponseDto
            {
                NodeId = file.NodeId,
                Key = key,
                UploadUrl = uploaded.Url,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
        }
        catch
        {
            throw new Exception("An error occurred while generating the presigned URL.");
        }
    }

    public async Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default)
    {
        return await _fileRepository.UpdateStatusAsync(confirmUploadDto.NodeId, UploadStatus.Completed, cancellationToken) is true
            ? new SuccessDto
            {
                Message = "File upload confirmed successfully."
            }
            : throw new NotFoundException($"File with NodeId {confirmUploadDto.NodeId} not found.");
    }
}
