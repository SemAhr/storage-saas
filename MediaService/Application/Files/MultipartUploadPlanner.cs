namespace MediaService.Application.Files;

public sealed record MultipartPlan(
    long PartSize,
    int PartsCount
);

public static class MultipartUploadPlanner
{
    private const long Mib = 1024 * 1024;

    public static MultipartPlan CreatePlan(
        long fileSize,
        long singleUploadMaxSize,
        long defaultPartSize,
        long minimumPartSize,
        long maximumPartSize,
        int maximumPartsCount)
    {
        if (fileSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size must be greater than zero.");
        }

        if (fileSize <= singleUploadMaxSize)
        {
            throw new InvalidOperationException("File size does not require multipart upload.");
        }

        var maximumFileSize = checked(maximumPartSize * maximumPartsCount);

        if (fileSize > maximumFileSize)
        {
            throw new InvalidOperationException("File is too large for the configured multipart upload policy.");
        }

        var requiredPartSize = DivideRoundUp(fileSize, maximumPartsCount);

        var selectedPartSize = Math.Max(defaultPartSize, requiredPartSize);
        selectedPartSize = Math.Max(selectedPartSize, minimumPartSize);
        selectedPartSize = RoundUpToMib(selectedPartSize);

        if (selectedPartSize > maximumPartSize)
        {
            throw new InvalidOperationException("File is too large for the configured maximum part size.");
        }

        var partsCount = CalculatePartsCount(fileSize, selectedPartSize);

        if (partsCount > maximumPartsCount)
        {
            throw new InvalidOperationException("Multipart upload exceeds the maximum number of allowed parts.");
        }

        return new MultipartPlan(PartSize: selectedPartSize, PartsCount: partsCount);
    }

    public static long CalculatePartSize(
        long fileSize,
        long basePartSize,
        int partNumber,
        int partsCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(basePartSize);

        if (partNumber < 1 || partNumber > partsCount)
        {
            throw new ArgumentOutOfRangeException(nameof(partNumber));
        }

        if (partNumber < partsCount)
        {
            return basePartSize;
        }

        var bytesBeforeLastPart = basePartSize * (partsCount - 1);

        return fileSize - bytesBeforeLastPart;
    }

    private static int CalculatePartsCount(long fileSize, long partSize)
    {
        return checked((int)DivideRoundUp(fileSize, partSize));
    }

    private static long DivideRoundUp(long value, long divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    private static long RoundUpToMib(long value)
    {
        return DivideRoundUp(value, Mib) * Mib;
    }
}
