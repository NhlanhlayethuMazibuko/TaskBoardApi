using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using TaskBoard.Api.Dtos;

namespace TaskBoard.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"taskboard-test-{Guid.NewGuid():N}.db");

    // has to match the JSON options in Program.cs
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools(); // otherwise the file stays locked
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

public static class TestHelpers
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;

    public static async Task<ProjectResponse> CreateProjectAsync(this HttpClient client, string name = "Test project")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name });
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ProjectResponse>();
    }

    public static async Task<TaskResponse> CreateTaskAsync(
        this HttpClient client, int projectId, string title, string? owner = null, string status = "Todo")
    {
        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new { title, owner, status });
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<TaskResponse>();
    }
}
