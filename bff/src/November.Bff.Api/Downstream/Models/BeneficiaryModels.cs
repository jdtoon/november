namespace November.Bff.Api.Downstream.Models;

public sealed record Beneficiary(
    string Id,
    string Name,
    string Country,
    string Currency,
    string BankName,
    string? Iban,
    string? AccountNumber,
    string? RoutingNumber,
    string SwiftBic,
    DateTimeOffset? LastPaidAt);

public sealed record NewBeneficiaryDetails(
    string Name,
    string Country,
    string Currency,
    string BankName,
    string? Iban,
    string? AccountNumber,
    string? RoutingNumber,
    string SwiftBic);
