namespace November.Bff.Api.Features.Session;

public sealed record CompanyResponse(string Id, string Name);

public sealed record SessionResponse(string UserId, string DisplayName, IReadOnlyList<string> Roles, CompanyResponse Company);
