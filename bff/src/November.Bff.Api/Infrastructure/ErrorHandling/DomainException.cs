namespace November.Bff.Api.Infrastructure.ErrorHandling;

public sealed class DomainException(
    string code,
    int status,
    string title,
    string detail,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(detail)
{
    public string Code { get; } = code;

    public int Status { get; } = status;

    public string Title { get; } = title;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}
