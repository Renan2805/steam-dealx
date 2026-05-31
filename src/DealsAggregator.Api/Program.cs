using DealsAggregator.Api.Endpoints;
using DealsAggregator.Clients.Extensions;
using DealsAggregator.Infrastructure.Extensions;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDealsAggregatorClients(builder.Configuration);
builder.Services.AddDealsAggregatorInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("api", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 60;
        o.QueueLimit = 0;
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseDealsAggregatorInfrastructure();
app.UseCors();
app.UseRateLimiter();
app.MapGamesEndpoints();

app.Run();
