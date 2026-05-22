using DevolveFacill.Core.Ports;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Adapters.Storage.Mock;

public class MockStorageService(ILogger<MockStorageService> logger) : IStorageService
{
    private readonly Dictionary<string, string> _store = new();

    public Task<string> UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        _store[key] = $"mock://storage/{key}";
        logger.LogInformation("[MOCK STORAGE] Uploaded {Key} ({ContentType})", key, contentType);
        return Task.FromResult(_store[key]);
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var url = _store.TryGetValue(key, out var stored) ? stored : $"mock://storage/{key}";
        return Task.FromResult($"{url}?expires={DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()}");
    }

    public Task DeleteFileAsync(string key, CancellationToken ct = default)
    {
        _store.Remove(key);
        logger.LogInformation("[MOCK STORAGE] Deleted {Key}", key);
        return Task.CompletedTask;
    }
}
