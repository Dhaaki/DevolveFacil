using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DevolveFacill.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Adapters.Storage;

public class S3StorageService : IStorageService
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IConfiguration config, ILogger<S3StorageService> logger)
    {
        _logger = logger;
        _bucket = config["Storage:S3:BucketName"] ?? "devolvefacill";

        var serviceUrl = config["Storage:S3:ServiceUrl"];
        var accessKey = config["Storage:S3:AccessKey"] ?? string.Empty;
        var secretKey = config["Storage:S3:SecretKey"] ?? string.Empty;
        var usePathStyle = bool.Parse(config["Storage:S3:UsePathStyle"] ?? "true");

        var clientConfig = new AmazonS3Config
        {
            ForcePathStyle = usePathStyle,
        };

        if (!string.IsNullOrEmpty(serviceUrl))
            clientConfig.ServiceURL = serviceUrl;
        else
            clientConfig.RegionEndpoint = RegionEndpoint.SAEast1;

        _client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            clientConfig);
    }

    public async Task<string> UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(request, ct);
        _logger.LogInformation("Uploaded {Key} to S3 bucket {Bucket}", key, _bucket);
        return $"s3://{_bucket}/{key}";
    }

    public async Task<string> GetSignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return await Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task DeleteFileAsync(string key, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(_bucket, key, ct);
        _logger.LogInformation("Deleted {Key} from S3 bucket {Bucket}", key, _bucket);
    }
}
