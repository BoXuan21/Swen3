using System.ComponentModel.DataAnnotations;

namespace Swen3.BatchProcessor.Configuration
{
    public class BatchProcessorOptions
    {
        public const string SectionName = "BatchProcessor";

        /// <summary>
        /// Path to the input folder where XML files are placed
        /// </summary>
        [Required]
        public string InputFolder { get; set; } = "./input";

        /// <summary>
        /// Path to the archive folder for processed files
        /// </summary>
        [Required]
        public string ArchiveFolder { get; set; } = "./archive";

        /// <summary>
        /// File pattern to match XML files (e.g., "access_*.xml")
        /// </summary>
        public string FilePattern { get; set; } = "access_*.xml";

        /// <summary>
        /// Cron expression for scheduling (default: daily at 01:00 AM)
        /// </summary>
        public string CronSchedule { get; set; } = "0 1 * * *";

        /// <summary>
        /// Whether to delete files after processing instead of archiving
        /// </summary>
        public bool DeleteAfterProcessing { get; set; } = false;
    }
}

