using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace November.Bff.Api.Infrastructure.ErrorHandling;

public static class Problems
{
    private const string TypeBase = "https://november.example/problems/";

    public static ProblemDetails Create(
        int status,
        string code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Type = TypeFor(code),
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["code"] = code;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    public static ProblemDetails From(DomainException exception) =>
        Create(exception.Status, exception.Code, exception.Title, exception.Message, exception.Errors);

    public static ProblemDetails From(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => CamelCasePath(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return Create(
            StatusCodes.Status400BadRequest,
            "validation_failed",
            "One or more validation errors occurred",
            "The request failed validation.",
            errors);
    }

    public static string TypeFor(string code) => TypeBase + code.Replace('_', '-');

    public static string DefaultCodeFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "bad_request",
        StatusCodes.Status401Unauthorized => "unauthenticated",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not_found",
        StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
        StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
        _ => status >= 500 ? "internal_error" : "error"
    };

    private static string CamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(CamelCase));

    private static string CamelCase(string segment)
    {
        if (segment.Length == 0 || char.IsLower(segment[0]))
        {
            return segment;
        }

        var bracket = segment.IndexOf('[');
        var name = bracket < 0 ? segment : segment[..bracket];
        var suffix = bracket < 0 ? string.Empty : segment[bracket..];
        return char.ToLowerInvariant(name[0]) + name[1..] + suffix;
    }
}
