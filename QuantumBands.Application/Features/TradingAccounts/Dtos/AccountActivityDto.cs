using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos
{
    public class AccountActivityDto
    {
        public required PaginationDto Pagination { get; set; }
        public required ActivityFiltersDto Filters { get; set; }
        public required List<ActivityDto> Activities { get; set; } = new();
        public required ActivitySummaryDto Summary { get; set; }
    }

    public class PaginationDto
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ActivityFiltersDto
    {
        public string Type { get; set; } = "all";
        public DateRangeDto? DateRange { get; set; }
        public bool IncludeSystem { get; set; } = false;
    }

    public class ActivityDto
    {
        public required string ActivityId { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Type { get; set; }
        public required string Category { get; set; }
        public required string Description { get; set; }
        public required ActivityDetailsDto Details { get; set; }
        public required string Status { get; set; }
        public required InitiatedByDto InitiatedBy { get; set; }
        public required RelatedEntitiesDto RelatedEntities { get; set; }
        public decimal? BalanceAfter { get; set; }
        public required ActivityMetadataDto Metadata { get; set; }
    }

    public class ActivityDetailsDto
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? FromValue { get; set; }
        public string? ToValue { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Location { get; set; }
        public string? DeviceInfo { get; set; }
    }

    public class InitiatedByDto
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public required string Role { get; set; }
        public bool IsSystemGenerated { get; set; }
    }

    public class RelatedEntitiesDto
    {
        public string? TransactionId { get; set; }
        public int? TradeTicket { get; set; }
        public string? ConfigurationId { get; set; }
    }

    public class ActivityMetadataDto
    {
        public required string Source { get; set; }
        public string? Version { get; set; }
        public string? SessionId { get; set; }
    }

    public class ActivitySummaryDto
    {
        public required Dictionary<string, ActivityTypeCountDto> TotalByType { get; set; } = new();
        public required FinancialSummaryDto FinancialSummary { get; set; }
        public required SecurityEventsDto SecurityEvents { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class ActivityTypeCountDto
    {
        public int Count { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class FinancialSummaryDto
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal NetFlow { get; set; }
        public int PendingTransactions { get; set; }
    }

    public class SecurityEventsDto
    {
        public int LoginAttempts { get; set; }
        public int FailedLogins { get; set; }
        public int PasswordChanges { get; set; }
        public int SuspiciousActivity { get; set; }
    }
}