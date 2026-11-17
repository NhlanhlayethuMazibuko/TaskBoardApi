# TaskBoardApi

A REST API for a task board, made with ASP.NET Core. My group kept losing track of who was doing what on our projects, so I made a backend where you can create a project, add tasks, give each task an owner and a due date, and mark it as Todo, InProgress or Done.

I mainly built this to learn how to write a proper API in .NET (status codes, validation, Swagger and tests).

**Built with:** .NET 10, EF Core, SQLite, Swagger, xUnit

## How to run it

You need the .NET 10 SDK installed.

```bash
dotnet run --project src/TaskBoard.Api
```

Then go to http://localhost:5080/swagger to test the endpoints.

The database is a SQLite file (`taskboard.db`). It gets made automatically the first time you run the app. If you want to reset everything just delete it.

## Running the tests

```bash
dotnet test
```

The tests start the API in memory with `WebApplicationFactory` and send real requests to it. They use a separate temp database so they don't mess up your real data.

## Endpoints

**Projects**

```
GET     /api/projects
POST    /api/projects
GET     /api/projects/{id}
PUT     /api/projects/{id}
DELETE  /api/projects/{id}     (this deletes the project's tasks too)
```

**Tasks**

```
GET     /api/projects/{projectId}/tasks
POST    /api/projects/{projectId}/tasks
GET     /api/tasks/{id}
PUT     /api/tasks/{id}
PATCH   /api/tasks/{id}/status
DELETE  /api/tasks/{id}
```


## What I learned

- How to set up a Web API project and organise it with separate test projects
- Returning the right status codes (201, 204, 400, 404)
- Writing integration tests, which was harder than I expected but really useful
