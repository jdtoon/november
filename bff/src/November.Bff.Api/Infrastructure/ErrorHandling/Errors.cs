using System.Globalization;

namespace November.Bff.Api.Infrastructure.ErrorHandling;

public static class Errors
{
    public static DomainException NotFound(string what, string id) =>
        new("not_found", StatusCodes.Status404NotFound, $"{what} not found", $"{what} '{id}' was not found.");

    public static DomainException Forbidden(string detail) =>
        new("forbidden", StatusCodes.Status403Forbidden, "You are not allowed to do this", detail);

    public static DomainException SelfApprovalForbidden() =>
        new("self_approval_forbidden", StatusCodes.Status403Forbidden, "You cannot approve your own payment",
            "Four-eyes principle: the person who captured a payment cannot approve or reject it.");

    public static DomainException InvalidStatus(string detail) =>
        new("invalid_status", StatusCodes.Status409Conflict, "Action not allowed in the current status", detail);

    public static DomainException FxQuoteRequired(string detail) =>
        new("fx_quote_required", StatusCodes.Status409Conflict, "An FX quote is required", detail);

    public static DomainException FxQuoteExpired(string quoteId, DateTimeOffset expiredAt) =>
        new("fx_quote_expired", StatusCodes.Status409Conflict, "The FX quote has expired",
            $"Quote {quoteId} expired at {expiredAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}. Request a new quote.");

    public static DomainException FxQuoteUsed(string quoteId) =>
        new("fx_quote_used", StatusCodes.Status409Conflict, "The FX quote has already been used",
            $"Quote {quoteId} was already used. Request a new quote.");

    public static DomainException FxQuoteMismatch(string detail) =>
        new("fx_quote_mismatch", StatusCodes.Status409Conflict, "The FX quote does not match the request", detail);

    public static DomainException InsufficientFunds(string detail) =>
        new("insufficient_funds", StatusCodes.Status422UnprocessableEntity, "Insufficient funds", detail);

    public static DomainException Validation(string field, string message) =>
        new("validation_failed", StatusCodes.Status400BadRequest, "One or more validation errors occurred",
            "The request failed validation.",
            new Dictionary<string, string[]> { [field] = [message] });
}
