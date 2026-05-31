using System.ComponentModel;

namespace SteamDealX.Core.Models;

/// <summary>Bundle ativo que contém o jogo.</summary>
public sealed record ActiveBundle(
    [property: Description("Título do bundle")]
    string Title,

    [property: Description("URL do bundle")]
    string Url,

    [property: Description("Loja que oferece o bundle")]
    string Store);
