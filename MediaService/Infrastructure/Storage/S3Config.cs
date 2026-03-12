namespace MediaService.Infrastructure.Storage;

public sealed class S3Config
{
    public const string SectionName = "S3";

    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public int PresignedExpiration { get; set; } = 0; // ms

    // s3 compatible
    public string Endpoint { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = false;
}
