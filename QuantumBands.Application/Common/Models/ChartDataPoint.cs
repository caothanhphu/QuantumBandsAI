// QuantumBands.Application/Common/Models/ChartDataPoint.cs
namespace QuantumBands.Application.Common.Models;

public class ChartDataPoint<T>
{
    public string Date { get; set; } = string.Empty;
    public T Value { get; set; } = default!;
}