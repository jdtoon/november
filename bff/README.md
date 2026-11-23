# November BFF

The .NET 10 Backend-for-Frontend for the November reference repository: ASP.NET Core minimal APIs, the mediator pattern (martinothamar/Mediator), FluentValidation, cookie authentication with mock personas, and in-memory stand-ins for the bank platforms it would normally call.

```bash
dotnet run --project src/November.Bff.Api     # http://localhost:5080, Scalar at /scalar
dotnet test --solution November.Bff.slnx
docker build -t november-bff .
```

Everything is fictional and all data is in memory.

The full guide (architecture, request lifecycle, how to add a feature, auth, errors, FX and four-eyes, Docker) is in [`docs/bff/README.md`](../docs/bff/README.md). The API contract is in [`docs/api-contract.md`](../docs/api-contract.md).
