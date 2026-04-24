# Task Management API

This is a .NET 8 REST API for managing tasks across multiple tenants. The implementation keeps the business rules in the domain/application layers, leaves the API layer focused on HTTP concerns, and uses EF Core with SQLite for local persistence.

The code is intentionally small, but the boundaries are the same ones I would use in a larger service: domain behavior is isolated, application services coordinate use cases, infrastructure owns database details, and the API layer handles request/response behavior.

## Project Layout

```text
TaskManagementApi/
|-- src/
|   |-- TaskManagement.Api
|   |-- TaskManagement.Application
|   |-- TaskManagement.Domain
|   `-- TaskManagement.Infrastructure
`-- tests/
    `-- TaskManagement.Tests
```

## Architecture

The solution follows a clean architecture style without over-engineering the assignment.

| Project | Purpose |
| --- | --- |
| `TaskManagement.Domain` | Task entity, enums, and domain exceptions |
| `TaskManagement.Application` | DTOs, service contracts, task use cases, repository abstractions |
| `TaskManagement.Infrastructure` | EF Core DbContext, repository implementation, tenant provider |
| `TaskManagement.Api` | Controllers, middleware, Swagger, composition root |
| `TaskManagement.Tests` | Unit tests around service behavior and domain rules |

The important dependency rule is that the domain does not know about ASP.NET Core, EF Core, or the database. The application layer works against interfaces. Infrastructure plugs in the actual EF Core implementation at runtime.

## What Is Implemented

- Create, read, update, status update, and delete task endpoints
- Tenant isolation through the `X-Tenant-Id` request header
- Soft delete instead of physical deletes
- EF Core global query filter for tenant and soft-deleted task filtering
- Centralized exception handling middleware
- Swagger in development
- Unit tests for the main business rules

## Business Rules

The following rules are enforced outside the controller layer:

- A high-priority task must have a due date.
- A completed task cannot be deleted.
- A completed task cannot be moved back to `InProgress`.
- A cancelled task is read-only.
- Every task operation must run in a tenant context.

## API

All task endpoints require a tenant header:

```http
X-Tenant-Id: tenant-a
```

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/tasks` | Returns tasks for the current tenant |
| `GET` | `/api/tasks/{id}` | Returns one task |
| `POST` | `/api/tasks` | Creates a task |
| `PUT` | `/api/tasks/{id}` | Updates task details |
| `PATCH` | `/api/tasks/{id}/status` | Updates task status |
| `DELETE` | `/api/tasks/{id}` | Soft-deletes a task |

Example:

```bash
curl -X POST http://localhost:5298/api/tasks \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-a" \
  -d "{\"title\":\"Prepare release checklist\",\"description\":\"Finalize deployment steps\",\"priority\":\"High\",\"dueDate\":\"2026-05-10T00:00:00Z\",\"assignedTo\":\"delivery-lead\"}"
```

## Running the Project

Required:

- .NET 8 SDK

From the repository root:

```bash
dotnet restore TaskManagementApi.sln
dotnet build TaskManagementApi.sln
dotnet test TaskManagementApi.sln
dotnet run --project src/TaskManagement.Api
```

Swagger is available in development at:

```text
http://localhost:5298/swagger
```

The API uses SQLite by default:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tasks.db"
  }
}
```

For this assignment, the database is created automatically on startup with `EnsureCreated`.

## Implementation Notes

- `TenantMiddleware` reads `X-Tenant-Id` once and stores it in `HttpContext.Items`.
- `TenantProvider` exposes the current tenant to infrastructure code.
- `AppDbContext` applies a global filter so normal queries only return records for the current tenant and ignore soft-deleted tasks.
- `TaskItem` owns state transitions such as status changes and delete validation.
- `TaskService` coordinates application use cases and validates cross-field rules such as high priority plus due date.
- `ExceptionMiddleware` maps domain errors to `400`, missing resources to `404`, and unexpected errors to `500`.

## Assumptions

- Tenant identity is trusted from `X-Tenant-Id` because authentication is outside the scope of this assignment.
- SQLite is used to keep local setup simple.
- `EnsureCreated` is acceptable for a runnable assignment. In a production deployment, I would use migrations.
- The repository pattern is used here to keep application tests independent from EF Core.

## Next Steps

Given more time, I would add:

- Authentication and tenant resolution from claims
- EF Core migrations
- Integration tests for middleware, filters, and controllers
- Pagination and filtering on task listing
- Optimistic concurrency for updates
