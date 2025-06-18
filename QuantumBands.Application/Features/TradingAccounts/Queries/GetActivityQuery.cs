using QuantumBands.Application.Features.TradingAccounts.Enums;
using System;

namespace QuantumBands.Application.Features.TradingAccounts.Queries
{
    public class GetActivityQuery
    {
        public ActivityType Type { get; set; } = ActivityType.All;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public ActivitySortBy SortBy { get; set; } = ActivitySortBy.Timestamp;
        public SortOrder SortOrder { get; set; } = SortOrder.Desc;
        public bool IncludeSystem { get; set; } = false;
    }

    public enum ActivityType
    {
        All,
        Deposits,
        Withdrawals,
        Logins,
        Configs,
        Trades
    }

    public enum ActivitySortBy
    {
        Timestamp,
        Type,
        Amount
    }

    public enum SortOrder
    {
        Asc,
        Desc
    }
}