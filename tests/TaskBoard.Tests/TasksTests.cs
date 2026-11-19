using System.Net;
using System.Net.Http.Json;
using TaskBoard.Api.Dtos;
using TaskBoard.Api.Models;

namespace TaskBoard.Tests;

public class TasksTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedTaskWithDefaults()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Draw the ERD", owner = "Thabo", dueDate = "2026-11-30" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.ReadAsync<TaskResponse>();
        Assert.Equal(project.Id, task.ProjectId);
        Assert.Equal("Draw the ERD", task.Title);
        Assert.Equal("Thabo", task.Owner);
        Assert.Equal(new DateOnly(2026, 11, 30), task.DueDate);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.EndsWith($"/api/tasks/{task.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_InMissingProject_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/projects/999999/tasks", new { title = "Orphan" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutTitle_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", new { owner = "Thabo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownStatus_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Bad status", status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_IsSentAsText()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Readable", status: "InProgress");

        var json = await _client.GetStringAsync($"/api/tasks/{task.Id}");

        Assert.Contains("\"status\":\"InProgress\"", json);
    }

    [Fact]
    public async Task GetForProject_ReturnsOnlyThatProjectsTasks()
    {
        var mine = await _client.CreateProjectAsync("Mine");
        var other = await _client.CreateProjectAsync("Other");
        await _client.CreateTaskAsync(mine.Id, "Mine 1");
        await _client.CreateTaskAsync(mine.Id, "Mine 2");
        await _client.CreateTaskAsync(other.Id, "Not mine");

        var tasks = await (await _client.GetAsync($"/api/projects/{mine.Id}/tasks")).ReadAsync<List<TaskResponse>>();

        Assert.Equal(["Mine 1", "Mine 2"], tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task GetForProject_FiltersByStatusAndOwner()
    {
        var project = await _client.CreateProjectAsync();
        await _client.CreateTaskAsync(project.Id, "A", owner: "Thabo", status: "Todo");
        await _client.CreateTaskAsync(project.Id, "B", owner: "Thabo", status: "Done");
        await _client.CreateTaskAsync(project.Id, "C", owner: "Lerato", status: "Done");
        var url = $"/api/projects/{project.Id}/tasks";

        var done = await (await _client.GetAsync($"{url}?status=Done")).ReadAsync<List<TaskResponse>>();
        var thabo = await (await _client.GetAsync($"{url}?owner=thabo")).ReadAsync<List<TaskResponse>>();
        var both = await (await _client.GetAsync($"{url}?status=Done&owner=Thabo")).ReadAsync<List<TaskResponse>>();

        Assert.Equal(["B", "C"], done.Select(t => t.Title));
        Assert.Equal(["A", "B"], thabo.Select(t => t.Title)); 
        Assert.Equal(["B"], both.Select(t => t.Title));
    }

    [Fact]
    public async Task GetForProject_WithUnknownStatus_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.GetAsync($"/api/projects/{project.Id}/tasks?status=Finished");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetForProject_WhenProjectMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/projects/999999/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/tasks/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesAllDetails()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Old title", owner: "Thabo");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new { title = "New title", status = "InProgress", dueDate = "2026-12-01" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await (await _client.GetAsync($"/api/tasks/{task.Id}")).ReadAsync<TaskResponse>();
        Assert.Equal("New title", fetched.Title);
        Assert.Equal(TaskItemStatus.InProgress, fetched.Status);
        Assert.Equal(new DateOnly(2026, 12, 1), fetched.DueDate);
        Assert.Null(fetched.Owner); // PUT replaces everything
    }

    [Fact]
    public async Task UpdateStatus_ChangesOnlyTheStatus()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Move me", owner: "Lerato");

        var response = await _client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await (await _client.GetAsync($"/api/tasks/{task.Id}")).ReadAsync<TaskResponse>();
        Assert.Equal(TaskItemStatus.Done, fetched.Status);
        Assert.Equal("Move me", fetched.Title);
        Assert.Equal("Lerato", fetched.Owner);
    }

    [Fact]
    public async Task UpdateStatus_WithoutStatus_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Stay put");

        var response = await _client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Delete me");

        var response = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/tasks/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_TrimsTitleAndOwner()
    {
        var project = await _client.CreateProjectAsync();

        var task = await _client.CreateTaskAsync(project.Id, "  Spaced out  ", owner: "  Thabo  ");

        Assert.Equal("Spaced out", task.Title);
        Assert.Equal("Thabo", task.Owner);
    }

    [Fact]
    public async Task Create_WithBlankOwner_StoresNoOwner()
    {
        var project = await _client.CreateProjectAsync();

        var task = await _client.CreateTaskAsync(project.Id, "Unassigned", owner: "   ");

        Assert.Null(task.Owner);
    }

    [Fact]
    public async Task Create_WithTitleOver200Characters_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = new string('x', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/tasks/999999", new { title = "Nobody" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutTitle_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Has a title");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { owner = "Thabo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync("/api/tasks/999999/status", new { status = "Done" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithUnknownStatus_ReturnsBadRequest()
    {
        var project = await _client.CreateProjectAsync();
        var task = await _client.CreateTaskAsync(project.Id, "Stay put");

        var response = await _client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
