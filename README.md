# Task Management API

Task Management API is a .NET 8 REST service for creating, updating, tracking, and deleting tasks in a multi-tenant environment. The application is intentionally compact, but it is structured with the same separation of concerns I would use in a production service: HTTP handling stays in the API layer, business rules live in the domain/application layers, and persistence details are isolated in infrastructure.

The API uses ASP.NET Core, EF Core, SQLite, Swagger, and xUnit. It supports tenant-aware task operations through the `X-Tenant-Id` request header and protects tenant data with an EF Core global query filter.

## About The Application

This application manages tasks with the following fields:

- Title
- Description
- Status
- Priority
- Due date
- Assignee
- Tenant id
- Created and updated timestamps
- Soft-delete state

The main goal of the assignment was to build a clean, maintainable task-management backend rather than placing all logic directly inside controllers. I implemented the application around clear use cases and domain rules so the code remains easy to test, extend, and reason about.

## What I Did In This Application

- Built a .NET 8 Web API with RESTful task endpoints.
- Added clean project separation across API, Application, Domain, Infrastructure, and Tests.
- Implemented CRUD functionality for task management.
- Added task status updates through a dedicated endpoint.
- Added tenant isolation using the `X-Tenant-Id` request header.
- Added EF Core SQLite persistence.
- Added EF Core global query filtering for tenant-specific and non-deleted records.
- Implemented soft delete instead of physically removing records.
- Moved business rules into the domain and application layers.
- Added centralized exception handling middleware.
- Configured Swagger for development-time API testing.
- Added xUnit unit tests for important business behavior.

## Application Layout And Architecture

The solution follows a clean architecture style. The dependency direction is inward: the API depends on Application, Application depends on Domain, and Infrastructure implements application contracts.

```text
TaskManagementApi/
|-- src/
|   |-- TaskManagement.Api
|   |-- TaskManagement.Application
|   |-- TaskManagement.Domain
|   `-- TaskManagement.Infrastructure
|-- tests/
|   `-- TaskManagement.Tests
|-- TaskManagementApi.sln
`-- README.md
```

| Project | Responsibility |
| --- | --- |
| `TaskManagement.Api` | Controllers, middleware, Swagger setup, dependency registration, HTTP request/response handling |
| `TaskManagement.Application` | DTOs, service interfaces, repository contracts, application use cases |
| `TaskManagement.Domain` | Task entity, enums, domain exceptions, task state rules |
| `TaskManagement.Infrastructure` | EF Core DbContext, SQLite repository, tenant provider, infrastructure registrations |
| `TaskManagement.Tests` | Unit tests for service behavior and domain rules |

### Request Flow

1. A client sends a request to `/api/tasks` with the `X-Tenant-Id` header.
2. `TenantMiddleware` reads the tenant id and stores it in the current HTTP context.
3. `TasksController` receives the request and delegates work to `ITaskService`.
4. `TaskService` validates application rules and coordinates the task use case.
5. `TaskItem` enforces domain behavior such as status transitions and delete rules.
6. `TaskRepository` persists changes through EF Core.
7. `AppDbContext` applies tenant and soft-delete filters automatically.
8. `ExceptionMiddleware` converts known exceptions into clean HTTP responses.

## Business Rules

The application currently enforces these rules:

- Every request must have a tenant context.
- A task title is required.
- A high-priority task must have a due date.
- A new task starts with `Pending` status.
- A completed task cannot be deleted.
- A completed task cannot be moved back to `InProgress`.
- A cancelled task is read-only.
- Deleted tasks are hidden from normal reads through soft delete.
- Tasks are isolated by tenant.

## API Endpoints

All endpoints require the tenant header:

```http
X-Tenant-Id: tenant-a
```

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/api/tasks` | Get all tasks for the current tenant |
| `GET` | `/api/tasks/{id}` | Get one task by id |
| `POST` | `/api/tasks` | Create a new task |
| `PUT` | `/api/tasks/{id}` | Update task details |
| `PATCH` | `/api/tasks/{id}/status` | Update only the task status |
| `DELETE` | `/api/tasks/{id}` | Soft-delete a task |

## How To Run The Application

### Prerequisites

- .NET 8 SDK

### Restore, Build, And Run

Run these commands from the repository root:

```bash
dotnet restore TaskManagementApi.sln
dotnet build TaskManagementApi.sln
dotnet run --project src/TaskManagement.Api
```

The HTTP profile runs on:

```text
http://localhost:5298
```

Swagger is available in development at:

```text
http://localhost:5298/swagger
```

The application uses SQLite by default:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tasks.db"
  }
}
```

For this assignment, the database is created automatically on startup using `EnsureCreated`. In a production service, I would replace this with EF Core migrations.

## How To Test The Application

Run the automated test suite from the repository root:

```bash
dotnet test TaskManagementApi.sln
```

The current test coverage focuses on application and domain behavior, including:

- High-priority task validation
- Tenant assignment during task creation
- Default task status
- Preventing deletion of completed tasks
- Preventing invalid status transitions

## How To Test The Functionality Manually

You can test the API through Swagger or with `curl`.

### 1. Start The API

```bash
dotnet run --project src/TaskManagement.Api
```

### 2. Create A Task

```bash
curl -X POST http://localhost:5298/api/tasks \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d "{\"title\":\"Prepare release checklist\",\"description\":\"Finalize deployment steps\",\"priority\":\"High\",\"dueDate\":\"2026-05-10T00:00:00Z\",\"assignedTo\":\"delivery-lead\"}"
```

Expected result:

- Status code `201 Created`
- Response body contains the created task
- Status should be `Pending`
- Tenant should be isolated to `tenant-a`

### 3. Get All Tasks For A Tenant

```bash
curl -X GET http://localhost:5298/api/tasks \
  -H "X-Tenant-Id: tenant-a"
```

Expected result:

- Status code `200 OK`
- Response contains tasks created under `tenant-a`

### 4. Verify Tenant Isolation

Run the same request with a different tenant:

```bash
curl -X GET http://localhost:5298/api/tasks \
  -H "X-Tenant-Id: tenant-b"
```

Expected result:

- Status code `200 OK`
- Tasks from `tenant-a` should not appear for `tenant-b`

### 5. Update A Task

Replace `{id}` with a real task id from the create response:

```bash
curl -X PUT http://localhost:5298/api/tasks/{id} \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d "{\"title\":\"Prepare release checklist v2\",\"description\":\"Update deployment steps\",\"priority\":\"Medium\",\"dueDate\":null,\"assignedTo\":\"backend-developer\"}"
```

Expected result:

- Status code `200 OK`
- Response contains the updated task details

### 6. Update Task Status

```bash
curl -X PATCH http://localhost:5298/api/tasks/{id}/status \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d "{\"status\":\"InProgress\"}"
```

Expected result:

- Status code `200 OK`
- Task status changes to `InProgress`

### 7. Soft Delete A Task

```bash
curl -X DELETE http://localhost:5298/api/tasks/{id} \
  -H "X-Tenant-Id: tenant-a"
```

Expected result:

- Status code `204 No Content`
- The task no longer appears in `GET /api/tasks`
- The row is soft-deleted instead of physically removed

### 8. Test Validation Behavior

Create a high-priority task without a due date:

```bash
curl -X POST http://localhost:5298/api/tasks \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d "{\"title\":\"Urgent task without due date\",\"description\":\"Should fail\",\"priority\":\"High\",\"dueDate\":null,\"assignedTo\":\"tester\"}"
```

Expected result:

- Status code `400 Bad Request`
- Error message explains that high-priority tasks must have a due date

## Implementation Notes

- `TenantMiddleware` reads `X-Tenant-Id` once per request.
- `TenantProvider` exposes the tenant id to application and infrastructure code.
- `AppDbContext` applies a global query filter for tenant isolation and soft delete.
- `TaskService` coordinates use cases and validates application-level rules.
- `TaskItem` owns domain behavior such as status transitions and delete validation.
- `ExceptionMiddleware` maps known application/domain errors to readable HTTP responses.

## Assumptions And Future Improvements

This assignment keeps infrastructure intentionally simple. Tenant identity is trusted from the request header because authentication and authorization are outside the current scope.

Given more time, I would add:

- Authentication and tenant resolution from user claims
- EF Core migrations instead of `EnsureCreated`
- Integration tests for controllers, middleware, and EF query filters
- Pagination, filtering, and sorting for task listing
- Optimistic concurrency for updates
- Docker support for repeatable local execution
