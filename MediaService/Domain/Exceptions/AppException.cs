namespace MediaService.Domain.Exceptions;

public enum AppErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    ExternalDependency,
    Infrastructure
}

public abstract class AppException(
    string message,
    AppErrorType errorType,
    Exception? innerException = null) : Exception(message, innerException)
{
    public AppErrorType ErrorType { get; } = errorType;
}

public sealed class ValidationException(string message, Exception? innerException = null) : AppException(message, AppErrorType.Validation, innerException)
{
}

public sealed class NotFoundException(string message, Exception? innerException = null) : AppException(message, AppErrorType.NotFound, innerException)
{
}

public sealed class ConflictException(string message, Exception? innerException = null) : AppException(message, AppErrorType.Conflict, innerException)
{
}

public sealed class UnauthorizedAppException(string message, Exception? innerException = null) : AppException(message, AppErrorType.Unauthorized, innerException)
{
}

public sealed class ForbiddenAppException(string message, Exception? innerException = null) : AppException(message, AppErrorType.Forbidden, innerException)
{
}

public sealed class ExternalDependencyException(string message, Exception? innerException = null) : AppException(message, AppErrorType.ExternalDependency, innerException)
{
}

public sealed class InfrastructureException(string message, Exception? innerException = null) : AppException(message, AppErrorType.Infrastructure, innerException)
{
}
