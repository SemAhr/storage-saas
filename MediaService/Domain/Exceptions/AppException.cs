namespace MediaService.Domain.Exceptions;

public abstract class AppException(string message, int statusCode, string? title = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? Title { get; } = title;
}

public sealed class NotFoundException(string message) : AppException(message, 404, "Not Found")
{
}

public sealed class ValidationException(string message) : AppException(message, 400, "Validation Error")
{
}

public sealed class ConflictException(string message) : AppException(message, 409, "Conflict")
{
}
