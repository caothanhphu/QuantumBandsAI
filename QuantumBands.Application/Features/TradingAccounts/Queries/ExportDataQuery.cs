using QuantumBands.Application.Features.TradingAccounts.Dtos;
using System;
using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.TradingAccounts.Queries
{
    /// <summary>
    /// Query parameters for exporting trading account data in various formats.
    /// Supports exporting different types of data including trading history, 
    /// statistics, performance reports, and risk reports.
    /// Part of SCRUM-101 implementation for data export functionality.
    /// </summary>
    public class ExportDataQuery
    {
        /// <summary>
        /// The type of data to export (TradingHistory, Statistics, PerformanceReport, RiskReport, Custom)
        /// </summary>
        [Required]
        public ExportType Type { get; set; }
        
        /// <summary>
        /// The format for the exported file (CSV, Excel, PDF)
        /// </summary>
        [Required]
        public ExportFormat Format { get; set; }
        
        /// <summary>
        /// Optional start date to filter data by date range.
        /// If not specified, all historical data will be included.
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Optional end date to filter data by date range.
        /// If not specified, data up to current date will be included.
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Optional comma-separated list of trading symbols to filter data.
        /// Example: "EURUSD,GBPUSD,USDJPY"
        /// Maximum length: 200 characters
        /// </summary>
        [MaxLength(200)]
        public string? Symbols { get; set; }
        
        /// <summary>
        /// Whether to include chart images in the export (for supported formats).
        /// Currently supported for PDF exports only.
        /// </summary>
        public bool IncludeCharts { get; set; } = false;
        
        /// <summary>
        /// Optional template name for custom export formatting.
        /// Maximum length: 50 characters
        /// </summary>
        [MaxLength(50)]
        public string? Template { get; set; }
        
        /// <summary>
        /// Optional email address to send the export file to.
        /// If specified, the export will be sent via email instead of direct download.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }
    }
}