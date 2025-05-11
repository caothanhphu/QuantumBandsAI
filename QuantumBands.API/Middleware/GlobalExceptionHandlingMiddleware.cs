// QuantumBands.API/Middleware/GlobalExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantumBands.API.Models; // Using ErrorDetails model

namespace QuantumBands.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment env) // Inject IHostEnvironment để biết môi trường hiện tại
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // Mặc định là 500

            // Tùy chỉnh StatusCode và Message dựa trên loại Exception (ví dụ)
            // Bạn có thể mở rộng phần này để xử lý các custom exception của bạn
            // Ví dụ:
            // if (ex is YourCustomNotFoundException)
            // {
            //     context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            //     message = ex.Message;
            // }
            // else if (ex is YourCustomValidationException validationEx)
            // {
            //     context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            //     message = validationEx.Message; // Hoặc thông tin chi tiết lỗi validation
            //     details = JsonSerializer.Serialize(validationEx.Errors); // Nếu có
            // }

            var errorDetails = new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = "An internal server error occurred. Please try again later." // Thông báo chung cho production
            };

            // Chỉ hiển thị chi tiết lỗi (stack trace) ở môi trường Development
            if (_env.IsDevelopment())
            {
                errorDetails.Message = ex.Message; // Hiển thị message gốc của exception
                errorDetails.Details = ex.StackTrace?.ToString();
            }

            // Có thể tùy chỉnh message dựa trên loại exception cụ thể ở đây nếu muốn
            // trước khi quyết định message cuối cùng cho production

            await context.Response.WriteAsync(errorDetails.ToString());
        }
    }
}