using MediaService.Application.Nodes;
using MediaService.Application.Storage;
using MediaService.Domain.Exceptions;
using MediaService.Domain.Files;
using MediaService.Domain.Nodes;
using MediaService.Presentation.Contracts.Files;
using MediaService.Presentation.Contracts.Shared;
using Microsoft.Extensions.Options;
using OneOf;

namespace MediaService.Application.Files;

public sealed class FileService(
    // INodeRepository nodeRepository,
    IFileRepository fileRepository,
    IStorageService storageService,
    IOptions<UploadOptions> fileConfig
) : IFileService
{
    // private readonly INodeRepository _nodeRepository = nodeRepository;
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly UploadOptions _uploadOptions = fileConfig.Value;

    public async Task<OneOf<SingleUploadDto, MultipartUploadDto>> UploadAsync(UploadDto uploadDto, CancellationToken cancellationToken = default)
    {
        if (uploadDto.Size > _uploadOptions.MaxFileSize)
        {
            throw new BadHttpRequestException($"File size exceeds the maximum allowed size of {_uploadOptions.MaxFileSize} bytes.");
        }

        var nodeId = Guid.NewGuid();
        var storageKey = _storageService.GenerateStorageKey(uploadDto.FileName, nodeId);
        var expiresAt = DateTime.UtcNow.Add(_uploadOptions.UploadExpiration);

        var file = new FileEntity
        {
            NodeId = nodeId,
            MimeType = uploadDto.MimeType,
            Size = uploadDto.Size,
            ObjectKey = storageKey,
            Status = UploadStatus.Pending,
            Node = new NodeEntity
            {
                Id = nodeId,
                ParentId = uploadDto.ParentId,
                Name = uploadDto.FileName,
                Type = NodeType.File
            }
        };

        // SINGLE UPLOAD
        if (uploadDto.Size <= _uploadOptions.SingleUploadMaxSize)
        {
            var sessionId = await _fileRepository.CreateSingleUploadSessionAsync(file, expiresAt, cancellationToken);
            var uploadUrl = await _storageService.GenerateSingleUploadUrlAsync(storageKey, uploadDto.MimeType, expiresAt);

            return new SingleUploadDto
            {
                NodeId = nodeId,
                SessionId = sessionId,
                UploadMode = UploadMode.Single,
                UploadUrl = uploadUrl,
                ExpiresAt = expiresAt
            };
        }

        // MULTIPART UPLOAD
        var multipartPlan = MultipartUploadPlanner.CreatePlan(
            fileSize: uploadDto.Size,
            singleUploadMaxSize: _uploadOptions.SingleUploadMaxSize,
            defaultPartSize: _uploadOptions.DefaultPartSize,
            minimumPartSize: _uploadOptions.MinPartSize,
            maximumPartSize: _uploadOptions.MaxPartSize,
            maximumPartsCount: _uploadOptions.MaxPartsCount
        );

        var multipartSessionId = await _fileRepository.CreateMultipartUploadSessionAsync(file, expiresAt, cancellationToken);

        try
        {
            await _fileRepository.AttachMultipartUploadPartAsync(
                multipartSessionId,
                storageKey,
                multipartPlan.PartSize,
                multipartPlan.PartsCount,
                cancellationToken
            );

            return new MultipartUploadDto
            {
                NodeId = nodeId,
                SessionId = multipartSessionId,
                UploadMode = UploadMode.Multipart,
                PartSize = multipartPlan.PartSize,
                PartsCount = multipartPlan.PartsCount,
                ExpiresAt = expiresAt
            };
        }
        catch
        {
            await _fileRepository.FinishMultipartUploadAsync(multipartSessionId, cancellationToken);

            throw;
        }
    }

    // public async Task<SuccessDto> ConfirmUploadAsync(ConfirmUploadDto confirmUploadDto, CancellationToken cancellationToken = default)
    // {
    //     return await _fileRepository.UpdateStatusAsync(confirmUploadDto.NodeId, UploadStatus.Completed, cancellationToken) is true
    //         ? new SuccessDto
    //         {
    //             Message = "File upload confirmed successfully."
    //         }
    //         : throw new NotFoundException($"File with NodeId {confirmUploadDto.NodeId} not found.");
    // }
}
