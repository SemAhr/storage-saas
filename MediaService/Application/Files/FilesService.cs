using MediaService.Application.Nodes;
using MediaService.Application.Storage;
using MediaService.Domain.Exceptions;
using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Shared;
using Microsoft.Extensions.Options;

namespace MediaService.Application.Files;

public sealed class FileService(
    INodeRepository nodeRepository,
    IFileRepository fileRepository,
    IStorageService storageService,
    IOptions<FileConfig> fileConfig
) : IFileService
{
    private readonly INodeRepository _nodeRepository = nodeRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly FileConfig _fileConfig = fileConfig.Value;

    public async Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default)
    {
        var nodeId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.Add(_fileConfig.UploadExpiration);

        // Generate a presigned URL for the file upload using the storage service and return it to the client
        var storageKey = _storageService.GenerateS3Key(presignedUploadDto.FileName, nodeId);
        var uploadUrl = _storageService.GenerateUploadUrl(storageKey, presignedUploadDto.MimeType, expiresAt);

        var node = new NodeEntity
        {
            Id = nodeId,
            ParentId = presignedUploadDto.ParentId,
            Name = presignedUploadDto.FileName,
            Type = NodeType.File,
            File = new FileEntity
            {
                MimeType = presignedUploadDto.MimeType,
                Size = presignedUploadDto.Size,
                ObjectKey = storageKey
            }
        };

        await _nodeRepository.AddAsync(node, cancellationToken);

        return new PresignedResponseDto
        {
            NodeId = nodeId,
            StorageKey = storageKey,
            UploadUrl = uploadUrl,
            ExpiresAt = expiresAt
        };
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
