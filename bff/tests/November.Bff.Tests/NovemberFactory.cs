using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace November.Bff.Tests;

public sealed class NovemberFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.Parse("2026-11-23T10:00:00Z"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Simulation:ProcessingDelaySeconds", "3600");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public async Task<HttpClient> SignInAsync(string personaId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-CSRF", "1");
        var response = await client.PostAsJsonAsync("/api/session/login", new { personaId }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }
}

public static class ApiExtensions
{
    public static async Task<(HttpResponseMessage Response, JsonNode Body)> PostJsonAsync(this HttpClient client, string url, object? body = null)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = body is null
            ? await client.PostAsync(url, null, cancellationToken)
            : await client.PostAsJsonAsync(url, body, cancellationToken);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        return (response, json!);
    }

    public static async Task<JsonNode> GetJsonAsync(this HttpClient client, string url)
    {
        var json = await client.GetFromJsonAsync<JsonNode>(url, TestContext.Current.CancellationToken);
        return json!;
    }
}
