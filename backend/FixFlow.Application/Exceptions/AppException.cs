namespace FixFlow.Application.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string message, int statusCode = 400, string code = "BAD_REQUEST") : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class NotFoundException(string message) : AppException(message, 404, "NOT_FOUND");

public class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException(message, 403, "FORBIDDEN");

public class ConflictException(string message) : AppException(message, 409, "CONFLICT");

public class UnauthorizedAppException(string message = "Invalid credentials.")
    : AppException(message, 401, "UNAUTHORIZED");

public class ValidationFailedException(string message)
    : AppException(message, 400, "VALIDATION_FAILED");
