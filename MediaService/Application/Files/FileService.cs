using MediaService.Application.Shared.Files;
using MediaService.Application.Shared.Nodes;
using MediaService.Application.Shared.Storage;
using MediaService.Contracts.Files;
using MediaService.Contracts.Nodes;
using MediaService.Contracts.Shared;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using MediaService.Domain.Exceptions;

namespace MediaService.Application.Files;

public sealed class FileService(INodeRepository nodeRepository, IFileRepository fileRepository, IStorageService storageService) : IFileService
{
    private readonly INodeRepository _nodeRepository = nodeRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;

    public async Task<PresignedResponseDto> PresignedUploadAsync(PresignedRequestDto presignedUploadDto, CancellationToken cancellationToken = default)
    {
        var node = new NodeEntity
        {
            ParentId = string.IsNullOrWhiteSpace(presignedUploadDto.ParentId)
                ? null
                : Guid.Parse(presignedUploadDto.ParentId),
            Name = presignedUploadDto.FileName,
            Type = NodeType.File,
            File = new FileEntity
            {
                MimeType = presignedUploadDto.MimeType,
                Size = presignedUploadDto.Size,
            }
        };

        var savedNode = await _nodeRepository.AddAsync(node, cancellationToken);

        // Generate a presigned URL for the file upload using the storage service and return it to the client
        var key = _storageService.GenerateS3Key(presignedUploadDto.FileName, savedNode.Id);
        var uploadUrl = await _storageService.GenerateUploadUrlAsync(key, presignedUploadDto.MimeType, cancellationToken);

        return new PresignedResponseDto
        {
            NodeId = node.Id,
            Key = key,
            UploadUrl = uploadUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }

    public async Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default)
    {
        var updatedFile = await _fileRepository.UpdateStatusAsync(Guid.Parse(confirmUploadDto.NodeId), UploadStatus.Success, cancellationToken);
        return updatedFile is null
            ? throw new NotFoundException($"File with NodeId {confirmUploadDto.NodeId} not found.")
            : new SuccessDto
            {
                Message = "File upload confirmed successfully."
            };
    }
}
