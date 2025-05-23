// QuantumBands.Application/Features/TradingAccounts/Queries/GetTradingAccountDetailsQuery.cs
namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetTradingAccountDetailsQuery
{
    public int ClosedTradesPageNumber { get; set; } = 1;
    public int ClosedTradesPageSize { get; set; } = 10;
    public int SnapshotsPageNumber { get; set; } = 1;
    public int SnapshotsPageSize { get; set; } = 10; // Có thể là 7 hoặc 30 tùy theo nhu cầu hiển thị
    public int OpenPositionsLimit { get; set; } = 20; // Giới hạn số lệnh mở

    private const int MaxPageSizeDefault = 50; // Max cho trades
    private const int MaxSnapshotsPageSize = 30; // Max cho snapshots
    private const int MaxOpenPositionsLimit = 50;

    public int ValidatedClosedTradesPageSize
    {
        get => (ClosedTradesPageSize > MaxPageSizeDefault || ClosedTradesPageSize <= 0) ? MaxPageSizeDefault : ClosedTradesPageSize;
        set => ClosedTradesPageSize = value;
    }
    public int ValidatedClosedTradesPageNumber
    {
        get => ClosedTradesPageNumber <= 0 ? 1 : ClosedTradesPageNumber;
        set => ClosedTradesPageNumber = value;
    }
    public int ValidatedSnapshotsPageSize
    {
        get => (SnapshotsPageSize > MaxSnapshotsPageSize || SnapshotsPageSize <= 0) ? MaxSnapshotsPageSize : SnapshotsPageSize;
        set => SnapshotsPageSize = value;
    }
    public int ValidatedSnapshotsPageNumber
    {
        get => SnapshotsPageNumber <= 0 ? 1 : SnapshotsPageNumber;
        set => SnapshotsPageNumber = value;
    }
    public int ValidatedOpenPositionsLimit
    {
        get => (OpenPositionsLimit > MaxOpenPositionsLimit || OpenPositionsLimit <= 0) ? MaxOpenPositionsLimit : OpenPositionsLimit;
        set => OpenPositionsLimit = value;
    }
}