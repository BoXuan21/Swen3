using Swen3.Storage.MiniIo;

namespace Swen3.Storage;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services
            .AddOptions<MinioOptions>()
            .Bind(builder.Configuration.GetSection("Minio"))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "MinIO endpoint must be configured")
            .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "MinIO bucket name must be configured")
            .ValidateOnStart();

        builder.Services.AddSingleton<IDocumentStorageService, MinioDocumentStorageService>();

        var host = builder.Build();
        host.Run();
    }
}
