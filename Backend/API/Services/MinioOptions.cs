namespace API.Services;

/// <summary>
/// Konfiguration for MinIO (S3-kompatibel objektlager).
/// Sæt i appsettings eller via env: Storage__MinIO__Endpoint, Storage__MinIO__AccessKey, Storage__MinIO__SecretKey, Storage__MinIO__BucketName.
/// I Docker kan AccessKey/SecretKey sættes til samme som MINIO_ROOT_USER / MINIO_ROOT_PASSWORD.
/// </summary>
public class MinioOptions
{
    public const string SectionName = "Storage:MinIO";

    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string BucketName { get; set; } = "kahoot-uploads";
    public bool UseSSL { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
