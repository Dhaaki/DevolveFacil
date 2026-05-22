namespace DevolveFacill.Core.Ports;

public interface IStorageService
{
    Task<string> UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteFileAsync(string key, CancellationToken ct = default);
}
