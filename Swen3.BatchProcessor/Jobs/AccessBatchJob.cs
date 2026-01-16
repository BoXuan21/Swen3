using Quartz;

namespace Swen3.BatchProcessor.Jobs;

/// <summary>
/// Quartz job that triggers the batch processor to run once.
/// </summary>
public class AccessBatchJob : IJob
{
    private readonly AccessLogBatchProcessor _processor;
    private readonly ILogger<AccessBatchJob> _logger;

    public AccessBatchJob(AccessLogBatchProcessor processor, ILogger<AccessBatchJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("[Quartz] Triggered at {Time}", DateTime.Now.ToString("HH:mm:ss"));
        await _processor.RunOnceAsync(context.CancellationToken);
    }
}

