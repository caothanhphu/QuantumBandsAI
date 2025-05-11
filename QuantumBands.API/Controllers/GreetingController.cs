using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Interfaces; // Thêm using cho IGreetingService

namespace QuantumBands.API.Controllers;

[ApiController]
[Route("[controller]")]
public class GreetingController : ControllerBase // Đổi tên controller cho phù hợp
{
    private readonly ILogger<GreetingController> _logger;
    private readonly IGreetingService _greetingService; // Inject IGreetingService

    public GreetingController(ILogger<GreetingController> logger, IGreetingService greetingService)
    {
        _logger = logger;
        _greetingService = greetingService; // Gán service đã inject
    }

    [HttpGet(Name = "GetGreeting")] // Đặt tên cho route
    public IActionResult Get(string name = "World") // Thêm tham số name
    {
        _logger.LogInformation("GreetingController.Get called with name: {Name}", name);
        string message = _greetingService.Greet(name);
        return Ok(message);
    }
    [HttpGet("error")]
    public IActionResult GetError()
    {
        _logger.LogInformation("Intentionally throwing an exception for testing global error handler.");
        throw new InvalidOperationException("This is a test exception for global error handler!");
    }

    [HttpGet("customerror")]
    public IActionResult GetCustomError()
    {
        _logger.LogInformation("Intentionally throwing a custom ApplicationException for testing.");
        // Bạn có thể tạo Custom Exception của riêng mình và xử lý nó trong middleware nếu muốn
        throw new ApplicationException("This is a custom application exception message.");
    }
}