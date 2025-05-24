// QuantumBands.API/Attributes/ApiKeyAuthorizeAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace QuantumBands.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-KEY";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails { Title = "API Key was not provided.", Status = StatusCodes.Status401Unauthorized });
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["AppSettings:EAIntegrationApiKey"];

        if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(potentialApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails { Title = "Invalid API Key.", Status = StatusCodes.Status401Unauthorized });
            return;
        }

        await next();
    }
}