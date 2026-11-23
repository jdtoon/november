namespace November.Bff.Api.Downstream.Models;

public sealed record Currency(string Code, string Name, int Decimals);

public sealed record Money(decimal Amount, string Currency);

public static class Currencies
{
    public static readonly IReadOnlyList<Currency> All =
    [
        new("GBP", "British pound", 2),
        new("EUR", "Euro", 2),
        new("USD", "US dollar", 2),
        new("ZAR", "South African rand", 2),
        new("CHF", "Swiss franc", 2),
        new("JPY", "Japanese yen", 0)
    ];

    public static bool IsSupported(string? code) =>
        code is not null && All.Any(c => c.Code == code);

    public static int DecimalsFor(string code) =>
        All.FirstOrDefault(c => c.Code == code)?.Decimals ?? 2;

    public static bool HasValidPrecision(decimal amount, string code) =>
        decimal.Round(amount, DecimalsFor(code)) == amount;

    public static decimal Round(decimal amount, string code, MidpointRounding mode = MidpointRounding.ToEven) =>
        decimal.Round(amount, DecimalsFor(code), mode);

    public static string Format(decimal amount, string code) =>
        $"{code} {amount.ToString("N" + DecimalsFor(code), System.Globalization.CultureInfo.InvariantCulture)}";
}
