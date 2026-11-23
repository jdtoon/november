namespace November.Bff.Api.Downstream.Models;

public sealed record Account(
    string Id,
    string Name,
    string AccountNumber,
    string SortCode,
    string Iban,
    string Currency,
    decimal Balance,
    decimal AvailableBalance);

public sealed record AccountRef(string Id, string Name, string Currency);
