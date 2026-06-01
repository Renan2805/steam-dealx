using SteamDealX.Api.Errors;
using SteamDealX.Clients.Extensions;
using SteamDealX.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSteamDealXClients(builder.Configuration);
builder.Services.AddSteamDealXInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
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
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        // "*" em Development permite qualquer origem (incluindo extensões Chrome em teste).
        // Em produção, configurar com o ID real da extensão: chrome-extension://<id>
        if (origins.Contains("*"))
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        // else: origins não configuradas → nenhuma request cross-origin permitida (seguro por padrão)
    }));

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
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
