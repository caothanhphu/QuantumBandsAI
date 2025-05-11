// QuantumBands.API/Models/ErrorDetails.cs
using System.Text.Json;

namespace QuantumBands.API.Models;

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; } // Sẽ chỉ có giá trị ở môi trường Development

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}