using System;
using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos
{
    /// <summary>
    /// Request DTO for initiating a data export operation.
    /// Contains all parameters needed to configure the export.
    /// Part of SCRUM-101 implementation.
    /// </summary>
    public class ExportRequestDto
    {
        [Required]
        public ExportType Type { get; set; }
        
        [Required]
        public ExportFormat Format { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [MaxLength(200)]
        public string? Symbols { get; set; }
        
        public bool IncludeCharts { get; set; } = false;
        
        [MaxLength(50)]
        public string? Template { get; set; }
        
        [EmailAddress]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Response DTO containing information about an export operation.
    /// Used for both synchronous and asynchronous export operations.
    /// </summary>
    public class ExportResponseDto
    {
        /// <summary>
        /// Unique identifier for the export operation
        /// </summary>
        public required string ExportId { get; set; }
        
        /// <summary>
        /// Current status of the export (Queued, Processing, Completed, Failed)
        /// </summary>
        public required string Status { get; set; }
        
        /// <summary>
        /// Estimated time for completion (for queued exports)
        /// </summary>
        public string? EstimatedTime { get; set; }
        
        /// <summary>
        /// URL to download the completed export file
        /// </summary>
        public string? DownloadUrl { get; set; }
        
        /// <summary>
        /// When the download URL expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Size of the exported file in bytes
        /// </summary>
        public long? FileSize { get; set; }
        
        /// <summary>
        /// Current progress percentage (0-100)
        /// </summary>
        public decimal Progress { get; set; }
        
        /// <summary>
        /// Name of the generated file
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// Error message if the export failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO for checking the status of an ongoing export operation.
    /// </summary>
    public class ExportStatusDto
    {
        public required string ExportId { get; set; }
        public required string Status { get; set; }
        public decimal Progress { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public long? FileSize { get; set; }
        public string? FileName { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Defines the type of data to export from trading accounts.
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// Export complete trading history with all trades
        /// </summary>
        TradingHistory,
        
        /// <summary>
        /// Export statistical analysis and performance metrics
        /// </summary>
        Statistics,
        
        /// <summary>
        /// Export comprehensive performance report with charts
        /// </summary>
        PerformanceReport,
        
        /// <summary>
        /// Export risk analysis and drawdown information
        /// </summary>
        RiskReport,
        
        /// <summary>
        /// Export using a custom template or format
        /// </summary>
        Custom
    }

    /// <summary>
    /// Supported file formats for data export.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Comma-separated values format - universally supported
        /// </summary>
        CSV,
        
        /// <summary>
        /// Microsoft Excel format with rich formatting
        /// </summary>
        Excel,
        
        /// <summary>
        /// Portable Document Format with charts and professional layout
        /// </summary>
        PDF
    }

    /// <summary>
    /// Status of an export operation throughout its lifecycle.
    /// </summary>
    public enum ExportStatus
    {
        /// <summary>
        /// Export request has been received and is waiting to be processed
        /// </summary>
        Queued,
        
        /// <summary>
        /// Export is currently being generated
        /// </summary>
        Processing,
        
        /// <summary>
        /// Export has been successfully completed and is ready for download
        /// </summary>
        Completed,
        
        /// <summary>
        /// Export failed due to an error
        /// </summary>
        Failed
    }

    /// <summary>
    /// Represents a single row in trading history export.
    /// Used for CSV and Excel exports of trading data.
    /// </summary>
    public class TradingHistoryExportRow
    {
        public string? Ticket { get; set; }
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
        public string? Symbol { get; set; }
        public string? Type { get; set; }
        public decimal Volume { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? Commission { get; set; }
        public decimal? Swap { get; set; }
        public decimal Profit { get; set; }
        public string? Comment { get; set; }
        public string? Duration { get; set; }
        public decimal? Pips { get; set; }
    }

    /// <summary>
    /// Container for the final export result with file data and metadata.
    /// Used internally to return export data from service layer.
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Binary data of the exported file
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// Suggested filename for the export
        /// </summary>
        public required string FileName { get; set; }
        
        /// <summary>
        /// MIME content type for the exported file
        /// </summary>
        public required string ContentType { get; set; }
        
        /// <summary>
        /// Size of the exported file in bytes
        /// </summary>
        public long FileSize => Data.Length;
    }
}