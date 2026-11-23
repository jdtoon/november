namespace November.Bff.Api.Infrastructure.Auth;

public sealed record Persona(string PersonaId, string DisplayName, IReadOnlyList<string> Roles, string Description);

public interface IPersonaDirectory
{
    IReadOnlyList<Persona> All { get; }

    CompanyRef Company { get; }

    Persona? Find(string personaId);
}

public sealed class MockPersonaDirectory : IPersonaDirectory
{
    public IReadOnlyList<Persona> All { get; } =
    [
        new("alex", "Alex Morgan", [Roles.Capturer], "Captures payments but cannot approve anything."),
        new("sam", "Sam Okafor", [Roles.Approver], "Approves or rejects what others captured."),
        new("jordan", "Jordan Reeves", [Roles.Capturer, Roles.Approver],
            "Can capture and approve, but never the same payment.")
    ];

    public CompanyRef Company { get; } = new("cmp_harbourline", "Harbourline Trading Ltd");

    public Persona? Find(string personaId) =>
        All.FirstOrDefault(p => string.Equals(p.PersonaId, personaId, StringComparison.OrdinalIgnoreCase));
}
