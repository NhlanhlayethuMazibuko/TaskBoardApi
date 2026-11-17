using System.Net;
using System.Net.Http.Json;
using TaskBoard.Api.Dtos;

namespace TaskBoard.Tests;

public class ProjectsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedProjectWithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "  Group project  ", description = "Semester 2" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await response.ReadAsync<ProjectResponse>();
        Assert.Equal("Group project", project.Name);
        Assert.Equal("Semester 2", project.Description);
        Assert.Equal(0, project.TaskCount);
        Assert.EndsWith($"/api/projects/{project.Id}", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithBlankName_ReturnsBadRequest(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNameOver100Characters_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsProjectWithTaskCount()
    {
        var created = await _client.CreateProjectAsync("Counted");
        await _client.CreateTaskAsync(created.Id, "One");
        await _client.CreateTaskAsync(created.Id, "Two");

        var project = await (await _client.GetAsync($"/api/projects/{created.Id}")).ReadAsync<ProjectResponse>();

        Assert.Equal("Counted", project.Name);
        Assert.Equal(2, project.TaskCount);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_IncludesCreatedProjects()
    {
        var a = await _client.CreateProjectAsync("List A");
        var b = await _client.CreateProjectAsync("List B");

        var projects = await (await _client.GetAsync("/api/projects")).ReadAsync<List<ProjectResponse>>();

        Assert.Contains(projects, p => p.Id == a.Id);
        Assert.Contains(projects, p => p.Id == b.Id);
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        var created = await _client.CreateProjectAsync("Old name");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new { name = "New name", description = "Now described" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await (await _client.GetAsync($"/api/projects/{created.Id}")).ReadAsync<ProjectResponse>();
        Assert.Equal("New name", fetched.Name);
        Assert.Equal("Now described", fetched.Description);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/projects/999999", new { name = "Nobody" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesProjectAndItsTasks()
    {
        var project = await _client.CreateProjectAsync("Doomed");
        var task = await _client.CreateTaskAsync(project.Id, "Goes too");

        var response = await _client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/projects/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
