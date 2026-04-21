using Amazon.S3;
using Amazon.S3.Model;
using MediaService.Application.Shared.Storage;
using Microsoft.Extensions.Options;
using System.Text;

namespace MediaService.Infrastructure.Storage;

public sealed class StorageService(IAmazonS3 s3Client, IOptions<S3Config> s3Options) : IStorageService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly S3Config _s3Config = s3Options.Value;

    public string GenerateS3Key(string fileName, Guid nodeId)
    {
        string safeFileName = SanitizeFileName(fileName);
        string extension = Path.GetExtension(safeFileName);
        string baseName = Path.GetFileNameWithoutExtension(safeFileName);
        string dateSegment = DateTime.UtcNow.ToString("yyyy/MM");

        return $"media/{dateSegment}/{nodeId:N}-{baseName}{extension}";
    }

    public Task<string> GenerateUploadUrlAsync(string key, string mimeType, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Config.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_s3Config.PresignedExpiration),
            ContentType = mimeType
        };

        string presignedUrl = _s3Client.GetPreSignedURL(request);

        return Task.FromResult(presignedUrl);
    }

    public Task<string> GenerateDownloadUrlAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _s3Config.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_s3Config.DownloadExpiration)
        };

        string presignedUrl = _s3Client.GetPreSignedURL(request);

        return Task.FromResult(presignedUrl);
    }

    public async Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(
            _s3Config.BucketName,
            key,
            cancellationToken);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        string onlyFileName = Path.GetFileName(fileName);
        var invalidCharacters = Path.GetInvalidFileNameChars();

        var builder = new StringBuilder(onlyFileName.Length);

        foreach (char currentCharacter in onlyFileName)
        {
            bool isInvalid = invalidCharacters.Contains(currentCharacter);
            builder.Append(isInvalid ? '-' : currentCharacter);
        }

        string sanitizedFileName = builder.ToString().Trim();

        if (string.IsNullOrWhiteSpace(sanitizedFileName))
            throw new ArgumentException("File name is invalid after sanitization.", nameof(fileName));

        return sanitizedFileName;
    }
}
