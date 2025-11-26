using System.ComponentModel.DataAnnotations;

namespace Swen3.Storage.MiniIo;

public class MinioOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;
}

