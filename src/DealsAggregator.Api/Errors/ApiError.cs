using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DealsAggregator.Api.Errors;

/// <summary>
/// Resposta de erro seguindo RFC 9457 Problem Details for HTTP APIs.
/// Todos os endpoints de erro retornam este schema com Content-Type: application/problem+json.
/// </summary>
public sealed record ApiError
{
    /// <summary>URI que identifica o tipo de problema. Referência à seção do RFC HTTP relevante.</summary>
    [JsonPropertyName("type")]
    [Description("URI que identifica o tipo de problema")]
    public string? Type { get; init; }

    /// <summary>Resumo legível do problema, invariável para o mesmo tipo de erro.</summary>
    [JsonPropertyName("title")]
    [Description("Resumo do problema")]
    public string? Title { get; init; }

    /// <summary>Código HTTP do erro.</summary>
    [JsonPropertyName("status")]
    [Description("Código HTTP")]
    public int? Status { get; init; }

    /// <summary>Explicação detalhada desta ocorrência específica do erro.</summary>
    [JsonPropertyName("detail")]
    [Description("Detalhe desta ocorrência do erro")]
    public string? Detail { get; init; }

    /// <summary>URI que identifica esta ocorrência específica do problema (o path da request).</summary>
    [JsonPropertyName("instance")]
    [Description("Path da request que gerou o erro")]
    public string? Instance { get; init; }

    /// <summary>
    /// Código legível por máquina para tratamento programático no client.
    /// Valores possíveis: GAME_NOT_FOUND, VALIDATION_ERROR, UPSTREAM_ERROR,
    /// UPSTREAM_RATE_LIMITED, RATE_LIMITED, NOT_IMPLEMENTED, INTERNAL_ERROR.
    /// </summary>
    [JsonPropertyName("code")]
    [Description("Código para tratamento programático. Ex: GAME_NOT_FOUND, UPSTREAM_ERROR, RATE_LIMITED.")]
    public string? Code { get; init; }
}
