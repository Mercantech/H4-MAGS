using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using API.Services;

namespace API.Controllers;

/// <summary>
/// Simpel controller til upload og hent af filer i MinIO (S3-kompatibel objektlager).
/// Kræver at Storage:MinIO er konfigureret (Endpoint, AccessKey, SecretKey, BucketName).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly MinioOptions _options;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IOptions<MinioOptions> options, ILogger<StorageController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private IMinioClient? CreateClient()
    {
        if (!_options.IsConfigured)
            return null;
        var endpoint = _options.Endpoint!.Trim();
        var withSsl = _options.UseSSL || endpoint.StartsWith("https:", StringComparison.OrdinalIgnoreCase);
        var hostPort = endpoint.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        return new MinioClient()
            .WithEndpoint(hostPort)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(withSsl)
            .Build();
    }

    /// <summary>
    /// Upload en fil til MinIO. Returnerer objektnøgle og URL til at hente filen via GET /api/storage/file/{key}.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<ActionResult<StorageUploadResponse>> Upload(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Ingen fil sendt." });

        var client = CreateClient();
        if (client == null)
            return StatusCode(503, new { message = "MinIO er ikke konfigureret (Storage:MinIO)." });

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        var contentType = file.ContentType ?? (extension switch { "jpg" or "jpeg" => "image/jpeg", "png" => "image/png", "gif" => "image/gif", "webp" => "image/webp", _ => "application/octet-stream" });
        var objectKey = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";

        try
        {
            await EnsureBucketExistsAsync(client, cancellationToken);

            using var stream = file.OpenReadStream();
            var putArgs = new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);
            await client.PutObjectAsync(putArgs, cancellationToken);

            var getUrl = Url.Action(nameof(GetFile), "Storage", new { key = objectKey }, Request.Scheme, Request.Host.Value) ?? "";
            _logger.LogInformation("Upload til MinIO: {Key} ({Size} bytes)", objectKey, file.Length);
            return Ok(new StorageUploadResponse { Key = objectKey, Url = getUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO upload fejlede for {Key}", objectKey);
            return StatusCode(500, new { message = "Upload fejlede.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Hent en fil fra MinIO efter objektnøgle. Auth: Anonymous (offentlige filer).
    /// </summary>
    [HttpGet("file/{*key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest();

        var client = CreateClient();
        if (client == null)
            return StatusCode(503, "MinIO er ikke konfigureret.");

        try
        {
            var memoryStream = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));
            await client.GetObjectAsync(getArgs, cancellationToken);
            memoryStream.Position = 0;

            var contentType = "application/octet-stream";
            if (key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) contentType = "image/jpeg";
            else if (key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) contentType = "image/png";
            else if (key.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) contentType = "image/gif";
            else if (key.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) contentType = "image/webp";

            Response.Headers.CacheControl = "public, max-age=3600";
            return File(memoryStream, contentType);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO get fejlede for {Key}", key);
            return StatusCode(500);
        }
    }

    private async Task EnsureBucketExistsAsync(IMinioClient client, CancellationToken cancellationToken)
    {
        var beArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        if (await client.BucketExistsAsync(beArgs, cancellationToken))
            return;
        var mbArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
        await client.MakeBucketAsync(mbArgs, cancellationToken);
        _logger.LogInformation("MinIO bucket oprettet: {Bucket}", _options.BucketName);
    }
}

public class StorageUploadResponse
{
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
