using Microsoft.EntityFrameworkCore;
using Quartz;
using Swen3.API.DAL;
using Swen3.BatchProcessor.Configuration;
using Swen3.BatchProcessor.Jobs;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services
    .AddOptions<BatchProcessorOptions>()
    .Bind(builder.Configuration.GetSection(BatchProcessorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Batch processor
builder.Services.AddSingleton<AccessLogBatchProcessor>();

// Get cron schedule from config
var cronSchedule = builder.Configuration.GetSection(BatchProcessorOptions.SectionName)
    .GetValue<string>("CronSchedule") ?? "0 1 * * ?";

// Quartz scheduling
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("AccessBatchJob");
    
    q.AddJob<AccessBatchJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("AccessBatchJob-trigger")
        .WithCronSchedule(cronSchedule));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();

// Apply migrations on startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    
    // Run once immediately on startup
    var processor = scope.ServiceProvider.GetRequiredService<AccessLogBatchProcessor>();
    await processor.RunOnceAsync();
}

host.Run();
