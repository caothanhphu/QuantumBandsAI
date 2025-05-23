// QuantumBands.Application/Features/Exchange/Queries/GetOrderBook/GetOrderBookQuery.cs
namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetOrderBookQuery
{
    public int Depth { get; set; } = 10; // Default depth

    private const int MaxDepth = 20;
    private const int MinDepth = 1;
    public int ValidatedDepth
    {
        get => (Depth > MaxDepth || Depth < MinDepth) ? MaxDepth : Depth; // Hoặc default là 10
        set => Depth = value;
    }
}