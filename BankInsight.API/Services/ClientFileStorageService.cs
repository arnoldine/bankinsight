using System.Text;

namespace BankInsight.API.Services;

public interface IClientFileStorageService
{
    bool IsInlineData(string value);
    Task<StoredClientFile> StoreAsync(string category, string fileName, string contentType, string dataUrl, CancellationToken cancellationToken = default);
    Task<StoredClientFileContent?> ReadAsync(string storageReference, string fallbackFileName, string fallbackContentType, CancellationToken cancellationToken = default);
}

public sealed record StoredClientFile(string StorageReference, long ByteCount, string StorageMode);

public sealed record StoredClientFileContent(byte[] Bytes, string FileName, string ContentType, string StorageMode);

public sealed class ClientFileStorageService : IClientFileStorageService
{
    private const string FileSystemPrefix = "fs://";
    private readonly string _mode;
    private readonly string _rootPath;

    public ClientFileStorageService(IConfiguration configuration, IHostEnvironment environment)
    {
        _mode = (configuration["ClientFileStorage:Mode"]
            ?? Environment.GetEnvironmentVariable("CLIENT_FILE_STORAGE_MODE")
            ?? "inline").Trim().ToLowerInvariant();

        var configuredRoot =
            configuration["ClientFileStorage:RootPath"]
            ?? Environment.GetEnvironmentVariable("CLIENT_FILE_STORAGE_ROOT");
        _rootPath = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "client-uploads")
            : configuredRoot;
    }

    public bool IsInlineData(string value) => value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    public async Task<StoredClientFile> StoreAsync(string category, string fileName, string contentType, string dataUrl, CancellationToken cancellationToken = default)
    {
        var bytes = DecodeDataUrl(dataUrl, contentType);
        if (_mode != "filesystem")
        {
            return new StoredClientFile(dataUrl, bytes.Length, "inline");
        }

        var safeCategory = SanitizePathSegment(category, "uncategorized");
        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? GuessExtension(contentType) : extension.ToLowerInvariant();
        var relativePath = Path.Combine(
            safeCategory,
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"),
            DateTime.UtcNow.ToString("dd"),
            $"{Guid.NewGuid():N}{safeExtension}");
        var absolutePath = Path.Combine(_rootPath, relativePath);

        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(absolutePath, bytes, cancellationToken);
        var normalizedReference = $"{FileSystemPrefix}{relativePath.Replace('\\', '/')}";
        return new StoredClientFile(normalizedReference, bytes.Length, "filesystem");
    }

    public async Task<StoredClientFileContent?> ReadAsync(string storageReference, string fallbackFileName, string fallbackContentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageReference))
        {
            return null;
        }

        if (IsInlineData(storageReference))
        {
            var bytes = DecodeDataUrl(storageReference, fallbackContentType);
            return new StoredClientFileContent(bytes, fallbackFileName, fallbackContentType, "inline");
        }

        if (!storageReference.StartsWith(FileSystemPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = storageReference[FileSystemPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootFullPath = Path.GetFullPath(_rootPath);
        if (!absolutePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolutePath))
        {
            return null;
        }

        var bytesFromDisk = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        return new StoredClientFileContent(bytesFromDisk, fallbackFileName, fallbackContentType, "filesystem");
    }

    private static byte[] DecodeDataUrl(string dataUrl, string expectedContentType)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex <= 0 || commaIndex == dataUrl.Length - 1)
        {
            throw new InvalidOperationException("Uploaded file payload is malformed.");
        }

        var header = dataUrl[..commaIndex];
        if (!header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded file payload must be base64 encoded.");
        }

        if (!string.IsNullOrWhiteSpace(expectedContentType) &&
            !header.Contains(expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploaded file content type does not match the payload header.");
        }

        try
        {
            return Convert.FromBase64String(dataUrl[(commaIndex + 1)..]);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Uploaded file payload is not valid base64 content.");
        }
    }

    private static string GuessExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/jpg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "application/pdf" => ".pdf",
        _ => ".bin"
    };

    private static string SanitizePathSegment(string value, string fallback)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(ch => !invalidChars.Contains(ch)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }
}
