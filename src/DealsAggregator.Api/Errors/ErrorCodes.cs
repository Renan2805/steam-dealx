namespace DealsAggregator.Api.Errors;

/// <summary>Códigos de erro usados no campo <c>code</c> das respostas ProblemDetails.</summary>
public static class ErrorCodes
{
    /// <summary>Jogo não encontrado em nenhuma fonte.</summary>
    public const string GameNotFound = "GAME_NOT_FOUND";

    /// <summary>Parâmetro de request inválido ou ausente.</summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>API upstream (gg.deals ou ITAD) retornou erro.</summary>
    public const string UpstreamError = "UPSTREAM_ERROR";

    /// <summary>API upstream atingiu rate limit.</summary>
    public const string UpstreamRateLimited = "UPSTREAM_RATE_LIMITED";

    /// <summary>Rate limit desta API foi atingido.</summary>
    public const string RateLimited = "RATE_LIMITED";

    /// <summary>Funcionalidade ainda não implementada.</summary>
    public const string NotImplemented = "NOT_IMPLEMENTED";

    /// <summary>Erro interno inesperado.</summary>
    public const string InternalError = "INTERNAL_ERROR";
}
