using DealsAggregator.Api.Errors;
using DealsAggregator.Clients.Extensions;
using DealsAggregator.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDealsAggregatorClients(builder.Configuration);
builder.Services.AddDealsAggregatorInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Garante que erros de model binding também usam ApiError (RFC 9457 + campo code)
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x => $"{e.Key}: {x.ErrorMessage}"));

            return new ObjectResult(new ApiError
            {
                Type     = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Status   = 400,
                Title    = "Validation Error",
                Detail   = string.Join("; ", errors),
                Instance = ctx.HttpContext.Request.Path,
                Code     = ErrorCodes.ValidationError
            })
            {
                StatusCode   = 400,
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", o =>
    {
        o.Window      = TimeSpan.FromMinutes(1);
        o.PermitLimit = 60;
        o.QueueLimit  = 0;
    });

    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new ApiError
        {
            Type     = "https://tools.ietf.org/html/rfc9110#section-15.5.29",
            Status   = 429,
            Title    = "Too Many Requests",
            Detail   = "API rate limit exceeded. Try again in a moment.",
            Instance = ctx.HttpContext.Request.Path,
            Code     = ErrorCodes.RateLimited
        }, ct);
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.Services.EnsureDatabase();
app.UseCors();
app.UseRateLimiter();
app.MapControllers();

app.Run();
