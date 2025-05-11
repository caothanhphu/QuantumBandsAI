// QuantumBands.Application/Services/GreetingService.cs
using QuantumBands.Application.Interfaces;
using Microsoft.Extensions.Logging; // Thêm using cho ILogger

namespace QuantumBands.Application.Services;

public class GreetingService : IGreetingService
{
    private readonly ILogger<GreetingService> _logger; // Khai báo logger

    // Constructor injection cho ILogger
    public GreetingService(ILogger<GreetingService> logger)
    {
        _logger = logger;
    }

    public string Greet(string name)
    {
        _logger.LogInformation("GreetingService.Greet called with name: {Name}", name); // Sử dụng logger
        return $"Hello, {name}! Welcome to QuantumBands AI.";
    }
}