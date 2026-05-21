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
    IFileRepository fileRepository,
    IStorageService storageService,
    IOptions<UploadOptions> fileConfig
) : IFileService
{
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
            var uploadUrl = _storageService.GenerateSingleUploadUrl(storageKey, uploadDto.MimeType, expiresAt);

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
            maximumPartsCount: _uploadOptions.MaxPartsCount);

        var multipartSessionId = await _fileRepository.CreateMultipartUploadSessionAsync(file, expiresAt, cancellationToken);

        string? storageUploadId = null;

        try
        {
            storageUploadId = await _storageService.GenerateMultipartUploadAsync(storageKey, uploadDto.MimeType, cancellationToken);

            await _fileRepository.AttachMultipartUploadPartAsync(
                sessionId: multipartSessionId,
                storageUploadId: storageKey,
                partSize: multipartPlan.PartSize,
                partsCount: multipartPlan.PartsCount,
                cancellationToken);

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
            if (!string.IsNullOrWhiteSpace(storageUploadId))
            {
                await _storageService.AbortMultipartUploadAsync(storageKey, storageUploadId, cancellationToken);
            }

            await _fileRepository.FinishMultipartUploadAsync(
                sessionId: multipartSessionId,
                status: UploadStatus.Failed,
                reason: "Failed to initialize multipart upload session.",
                cancellationToken);

            throw;
        }
    }

    public async Task<IReadOnlyList<PartUploadUrlDto>> GetPartsAsync(Guid sessionId, int from, int to, CancellationToken cancellationToken = default)
    {
        var sessionDetails = await _fileRepository.GetMultipartUploadSessionDetails(sessionId, cancellationToken);

        var partNumbers = Enumerable.Range(from, to - from + 1);

        return _storageService.GenerateMultipartUploadUrls(
            key: sessionDetails.ObjectKey,
            uploadId: sessionDetails.StorageUploadId,
            partNumbers: partNumbers,
            expiresAt: sessionDetails.ExpiresAt);
    }

    public async Task<UploadedPartsResponseDto> ConfirmPartsAsync(Guid sessionId, IReadOnlyList<UploadPartDto> parts, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionDetails = await _fileRepository.GetMultipartUploadSessionDetails(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new NotFoundException(ex.Message);
        }
    }

    public async Task<SuccessDto> AbortUploadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
    }

    public async Task<SuccessDto> ConfirmUploadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
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
