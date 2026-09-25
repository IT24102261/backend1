using FixFlow.Application.Exceptions;

namespace FixFlow.Application.Common;

public static class UploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> Documents = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    public static void EnsureImage(string contentType, long length) => Ensure(contentType, length, Images);

    public static void EnsureDocument(string contentType, long length) => Ensure(contentType, length, Documents);

    private static void Ensure(string contentType, long length, HashSet<string> allowed)
    {
        if (length <= 0 || length > MaxBytes)
        {
            throw new ValidationFailedException("File must be between 1 byte and 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || !allowed.Contains(contentType))
        {
            throw new ValidationFailedException("Unsupported file type.");
        }
    }
}
