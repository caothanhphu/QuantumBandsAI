// QuantumBands.API/Middleware/MiddlewareExtensions.cs
using Microsoft.AspNetCore.Builder;

namespace QuantumBands.API.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}