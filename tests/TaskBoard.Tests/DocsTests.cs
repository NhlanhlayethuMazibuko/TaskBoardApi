using System.Net;

namespace TaskBoard.Tests;

public class DocsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerJson_DescribesTheEndpoints()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("Task Board API", json);
        Assert.Contains("/api/projects/{projectId}/tasks", json);
        Assert.Contains("Lists all projects, newest first.", json);
    }

    [Fact]
    public async Task SwaggerPage_Loads()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
