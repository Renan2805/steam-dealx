using System.Net;
using System.Net.Mime;
using SteamDealX.Clients;
using Microsoft.AspNetCore.Diagnostics;

namespace SteamDealX.Api.Errors;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, code, title, detail) = exception switch
        {
            UpstreamApiException ex when ex.StatusCode == (int)HttpStatusCode.TooManyRequests =>
                (429, ErrorCodes.UpstreamRateLimited,
                 "Upstream Rate Limited",
                 $"{ex.UpstreamSource} rate limit reached. Try again shortly."),

            UpstreamApiException ex =>
                (502, ErrorCodes.UpstreamError,
                 "Upstream API Error",
                 $"{ex.UpstreamSource} returned an error. Try again shortly."),

            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } =>
                (429, ErrorCodes.UpstreamRateLimited,
                 "Upstream Rate Limited",
                 "An upstream API rate limit was reached. Try again shortly."),

            HttpRequestException =>
                (502, ErrorCodes.UpstreamError,
                 "Upstream Connectivity Error",
                 "Failed to reach an upstream API. Try again shortly."),

            NotImplementedException =>
                (501, ErrorCodes.NotImplemented,
                 "Not Implemented",
                 exception.Message),

            _ =>
                (500, ErrorCodes.InternalError,
                 "Internal Server Error",
                 "An unexpected error occurred.")
        };

        logger.LogError(exception,
            "Unhandled {ExceptionType} → {Code} ({Status})", exception.GetType().Name, code, status);

        httpContext.Response.StatusCode  = status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(new ApiError
        {
            Type     = $"https://tools.ietf.org/html/rfc9110#section-15.{status / 100}.{status % 100 / 10}",
            Status   = status,
            Title    = title,
            Detail   = detail,
            Instance = httpContext.Request.Path,
            Code     = code
        }, ct);

        return true;
    }
}
