using Amazon.S3;
using Amazon.S3.Model;
using MediaService.Application.Storage;
using Microsoft.Extensions.Options;
using System.Text;

namespace MediaService.Infrastructure.Storage;

public sealed class StorageService(IAmazonS3 s3Client, IOptions<S3Options> s3Options) : IStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly S3Options _s3Config = s3Options.Value;

    public static long CalculateBasePartSize(long fileSize, long defaultPartSize, long minimumPartSize, long maximumPartSize, int maximumPartsCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize);

        var requiredPartSize = (fileSize + maximumPartsCount - 1) / maximumPartsCount;

        var selectedPartSize = Math.Max(defaultPartSize, requiredPartSize);
        selectedPartSize = Math.Max(selectedPartSize, minimumPartSize);

        if (selectedPartSize > maximumPartSize)
        {
            throw new InvalidOperationException("File is too large for multipart upload.");
        }

        return selectedPartSize;
    }

    public string GenerateStorageKey(string fileName, Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("File extension is required.", nameof(fileName));
        }

        return $"media/{nodeId:N}/original{extension}";
    }

    public async Task<string> GenerateSingleUploadUrlAsync(string key, string mimeType, DateTime expiresAt)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Config.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = mimeType
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<string> GenerateMultipartUploadUrlAsync(string key, string mimeType, DateTime expiresAt)
    {
        var request = new InitiateMultipartUploadRequest
        {
            BucketName = _s3Config.BucketName,
            Key = key,
            ContentType = mimeType
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(request);
        return response.UploadId;
    }

    public string GenerateDownloadUrl(string key, DateTime expiresAt)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Config.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(
                _s3Config.BucketName,
                key,
                cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // If the object is not found, we can consider it as already deleted, so we return true.
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
