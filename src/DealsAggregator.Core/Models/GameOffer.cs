namespace DealsAggregator.Core.Models;

public enum OfferType { Retail, Keyshop }

public sealed record GameOffer(
    string Store,
    decimal Price,
    decimal? Regular,
    int CutPercent,
    string Url,
    OfferType Type,
    string Region);
