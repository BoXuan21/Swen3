using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swen3.API.DAL;
using Swen3.API.DAL.Models;
using Swen3.BatchProcessor.Configuration;
using System.Xml.Linq;

namespace Swen3.BatchProcessor.Jobs;

/// <summary>
/// Processes XML access log files from the input folder.
/// </summary>
public class AccessLogBatchProcessor
{
    private readonly ILogger<AccessLogBatchProcessor> _logger;
    private readonly BatchProcessorOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public AccessLogBatchProcessor(
        ILogger<AccessLogBatchProcessor> logger,
        IOptions<BatchProcessorOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch processing at {Time}", DateTime.UtcNow);

        // Ensure directories exist
        Directory.CreateDirectory(_options.InputFolder);
        Directory.CreateDirectory(_options.ArchiveFolder);

        var files = Directory.GetFiles(_options.InputFolder, _options.FilePattern);
        
        if (files.Length == 0)
        {
            _logger.LogInformation("No files to process");
            return;
        }

        _logger.LogInformation("Found {Count} file(s) to process", files.Length);

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessFileAsync(file, cancellationToken);
        }

        _logger.LogInformation("Batch processing completed at {Time}", DateTime.UtcNow);
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        _logger.LogInformation("Processing file: {FileName}", fileName);

        try
        {
            var entries = ParseXmlFile(filePath);
            if (entries == null)
            {
                MoveToErrorFolder(filePath);
                return;
            }

            await SaveToDatabase(entries, cancellationToken);
            ArchiveFile(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", fileName);
            MoveToErrorFolder(filePath);
        }
    }

    private List<(Guid DocumentId, int AccessCount, DateTime AccessDate, string? Source)>? ParseXmlFile(string filePath)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var entries = new List<(Guid, int, DateTime, string?)>();

            var entriesElement = doc.Root?.Element("Entries");
            if (entriesElement == null) return entries;

            foreach (var entry in entriesElement.Elements("Entry"))
            {
                if (!Guid.TryParse(entry.Element("DocumentId")?.Value, out var docId)) continue;
                if (!int.TryParse(entry.Element("AccessCount")?.Value, out var count)) continue;
                
                var date = DateTime.TryParse(entry.Element("AccessDate")?.Value, out var d) 
                    ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc) 
                    : DateTime.UtcNow.Date;
                
                entries.Add((docId, count, date, entry.Element("Source")?.Value));
            }

            _logger.LogInformation("Parsed {Count} entries from {File}", entries.Count, filePath);
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse XML file: {FilePath}", filePath);
            return null;
        }
    }

    private async Task SaveToDatabase(
        List<(Guid DocumentId, int AccessCount, DateTime AccessDate, string? Source)> entries,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var entry in entries)
        {
            // Skip if document doesn't exist
            if (!await db.Documents.AnyAsync(d => d.Id == entry.DocumentId, cancellationToken))
            {
                _logger.LogWarning("Document {Id} not found, skipping", entry.DocumentId);
                continue;
            }

            var existing = await db.DocumentAccessLogs
                .FirstOrDefaultAsync(l => l.DocumentId == entry.DocumentId && l.AccessDate == entry.AccessDate, cancellationToken);

            if (existing != null)
            {
                existing.AccessCount += entry.AccessCount;
                existing.LastUpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(entry.Source) && !existing.Sources.Contains(entry.Source))
                    existing.Sources = string.IsNullOrEmpty(existing.Sources) ? entry.Source : $"{existing.Sources},{entry.Source}";
            }
            else
            {
                db.DocumentAccessLogs.Add(new DocumentAccessLog
                {
                    DocumentId = entry.DocumentId,
                    AccessDate = entry.AccessDate,
                    AccessCount = entry.AccessCount,
                    Sources = entry.Source ?? "",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Saved {Count} entries to database", entries.Count);
    }

    private void ArchiveFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        if (_options.DeleteAfterProcessing)
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted: {FileName}", fileName);
        }
        else
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var archivePath = Path.Combine(_options.ArchiveFolder, 
                $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}.xml");
            File.Move(filePath, archivePath);
            _logger.LogInformation("Archived: {FileName}", fileName);
        }
    }

    private void MoveToErrorFolder(string filePath)
    {
        var errorFolder = Path.Combine(_options.ArchiveFolder, "errors");
        Directory.CreateDirectory(errorFolder);
        
        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var errorPath = Path.Combine(errorFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}_error.xml");
        File.Move(filePath, errorPath);
        _logger.LogWarning("Moved to errors: {FileName}", fileName);
    }
}

