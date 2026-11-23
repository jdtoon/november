namespace November.Bff.Api.Downstream.Models;

public sealed record Country(string Code, string Name, string Currency, BankDetailsFormat BankDetailsFormat);

public sealed record PurposeCode(string Code, string Description, PurposeDirection Direction);
