# LMS Content Service

Course-content microservice for the LMS platform. Owns the **module → lesson → resource**
hierarchy: instructors create and organize modules and lessons, students read published
content, and other services query content over an internal API. Built with ASP.NET Core
on **.NET 10** following Clean Architecture with a DDD aggregate model.

This service does **not** own courses themselves (only an `int CourseId` reference) and
does **not** store files — file bytes live in the **File Service**, which Content talks to
over HTTP and references by `FileId`.

---

## Tech stack

- **.NET 10** / ASP.NET Core Web API
- **Entity Framework Core 10** + SQL Server (schema-isolated under `Content`)
- **JWT Bearer** authentication (validates tokens issued by the Auth service)
- **Role-based authorization** (`Student` read, `Instructor` / `Admin` write)
- **API-key middleware** for internal service-to-service endpoints
- **HTTP client** (`IHttpClientFactory`) to the **File Service**
- **Serilog** structured logging
- **Scalar** for interactive API docs (OpenAPI)
- **xUnit** for tests

---

## Architecture

Four layers plus two test projects, with dependencies pointing inward
(`Api → Application → Domain`, `Infrastructure → Application/Domain`).

```
Lms.ContentService/
├── Lms.ContentService.Api              # Controllers, Program.cs, DI wiring, config
├── Lms.ContentService.Application       # ContentService, DTOs, app interfaces, pagination
├── Lms.ContentService.Domain            # Entities, enums, IContentRepository, PaginatedList
├── Lms.ContentService.Infrastructure    # EF Core, repository, File Service client, middleware
├── Lms.ContentService.UnitTests         # Domain tests (CourseModule, Lesson)
└── Lms.ContentService.IntegrationTests  # Controller + middleware tests (in-memory DB)
```

- **Domain** — `CourseModule` is the **aggregate root**; `Lesson` and `LessonResource`
  only exist inside a module and are reached through it. Entities are rich (private
  setters, behavior methods like `AddLesson()`, `Publish()`, `AttachResource()`,
  `ValidateOrder()`). `BaseEntity` supplies a `Guid Id` and audit timestamps and
  implements identity-based equality.
- **Application** — `ContentService` orchestrates the use cases and depends only on
  interfaces (`IContentRepository`, `IFileServiceClient`, `IApplicationDbContext`). It
  maps entities to DTOs and applies the published-only visibility rule for students.
- **Infrastructure** — `ContentDbContext`, `ContentRepository`, `FileServiceClient`
  (HTTP), and `ApiKeyMiddleware`. All wired via `AddInfrastructure(configuration)`.
- **Api** — three thin controllers split by audience (student / instructor / internal);
  JWT validation, the API-key middleware, and migration-on-startup live in `Program.cs`.

---

## Domain model

```
CourseModule (aggregate root)        Lesson                     LessonResource
─────────────────────────────        ──────────────────────     ──────────────────
CourseId (int, → Course svc)         ModuleId (FK)              LessonId (FK)
Title, Description, Order             Title, Content, VideoUrl   FileId (→ File svc)
Lessons (1..*)                       Order, DurationMinutes     ResourceType
                                     Status (Draft/Published/   AttachedAt
                                            Archived)
                                     Resources (0..*)
```

- **Ordering** is unique within its parent: module `Order` is unique per course, lesson
  `Order` is unique per module — enforced both in the domain (`ValidateOrder` /
  `ValidateUniqueOrder` → `InvalidOperationException`) and by unique DB indexes.
- **Lesson lifecycle:** `Draft → Published → Archived`. `Publish()` throws if the lesson
  is `Archived`. Only `Published` lessons are `IsAccessible` (visible to students).
- **Resources** reference a File Service `FileId`; the same file can't be attached twice
  to one lesson. Attaching first checks `IFileServiceClient.FileExistsAsync`.
- **Cascade delete:** deleting a module deletes its lessons, and deleting a lesson
  deletes its resources.

---

## Security model

The service exposes three audiences, each with its own controller and access rule:

| Controller | Route prefix | Access | Notes |
|---|---|---|---|
| `StudentContentController` | `/api/content` | `[AllowAnonymous]` (read) | Students see **published** content only; instructors/admins see drafts too (role is read from the token when present) |
| `InstructorContentController` | `/api/content` | `[Authorize(Roles = "Instructor,Admin")]` | Create/update/publish/attach. `DELETE module` is `Admin`-only |
| `InternalContentController` | `/api/internal/content` | **API key** (`X-Api-Key`) | Service-to-service; not behind JWT |

`ApiKeyMiddleware` guards any path under `/api/internal`: a missing key returns
`401 { "error": "api_key_missing" }` and a wrong key returns
`403 { "error": "invalid_api_key" }`. The expected value is read from
`InternalApiKeys:ContentService`.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or a full instance)
- A reachable **File Service** *(only needed to attach resources)*

### Run locally

```bash
# from the Lms.ContentService folder
dotnet restore
dotnet run --project Lms.ContentService.Api
```

The API listens on `http://localhost:5220` (and `https://localhost:7170` with the
`https` profile). On startup it applies EF Core migrations on a relational provider
(`Database.Migrate()`), or calls `EnsureCreated()` on a non-relational one.

Once running:

- API root health string → `http://localhost:5220/`
- Health check → `http://localhost:5220/health`
- Interactive docs (Scalar) → `http://localhost:5220/scalar/v1`
- OpenAPI document → `http://localhost:5220/openapi/v1.json`

---

## Configuration

Settings are read from `appsettings.json` and can be overridden by environment variables
or user secrets. The committed file ships secrets **empty** on purpose — supply them
before running.

| Section | Key | Description |
|---|---|---|
| `ConnectionStrings` | `ContentDb` | SQL Server connection string |
| `Jwt` | `Secret` | Signing key shared with the Auth service. **Required at startup** |
| `Jwt` | `Issuer` | Expected token issuer (default `lms-auth-service`) |
| `Jwt` | `Audience` | Expected token audience (default `lms-api`) |
| `ServiceUrls` | `FileService` | Base URL of the File Service |
| `InternalApiKeys` | `ContentService` | Key this service requires on `/api/internal` calls |
| `InternalApiKeys` | `FileService` | Key this service sends to the File Service |

> `Program.cs` throws on startup only if `Jwt:Secret` is missing. The connection string
> and internal API keys are not validated at startup, so a bad/empty value surfaces at
> first use rather than on boot.

---

## API reference

Base route: `/api/content` (public + instructor) and `/api/internal/content` (service).

### Read (public)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/content/courses/{courseId}/modules` | Anonymous | Paginated modules for a course |
| `GET` | `/api/content/modules/{moduleId}/lessons` | Anonymous | Paginated lessons for a module |
| `GET` | `/api/content/lessons/{lessonId}` | Anonymous | A single lesson |

Students see only published lessons; a request carrying an `Instructor`/`Admin` token
also sees drafts.

### Write (Instructor / Admin)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/content/courses/{courseId}/modules` | Instructor/Admin | Create a module |
| `PUT` | `/api/content/modules/{moduleId}` | Instructor/Admin | Update a module |
| `DELETE` | `/api/content/modules/{moduleId}` | **Admin** | Delete a module (cascades) |
| `POST` | `/api/content/modules/{moduleId}/lessons` | Instructor/Admin | Create a lesson |
| `PUT` | `/api/content/lessons/{lessonId}` | Instructor/Admin | Update a lesson |
| `POST` | `/api/content/lessons/{lessonId}/publish` | Instructor/Admin | Publish a lesson |
| `POST` | `/api/content/lessons/{lessonId}/resources` | Instructor/Admin | Attach a File Service file |

Duplicate `Order` values return `409 Conflict` (`duplicate_order` /
`duplicate_module_order` / `duplicate_lesson_order`). Missing entities return
`404 Not Found`.

### Internal (API key)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/internal/content/courses/{courseId}/has-content` | `X-Api-Key` | Whether the course has any published lessons |
| `GET` | `/api/internal/content/lessons/{lessonId}/info` | `X-Api-Key` | Lesson details for other services |

### Example: create a module

```http
POST /api/content/courses/42/modules
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "title": "Getting Started",
  "description": "Intro module",
  "order": 1
}
```

Returns `201 Created` with a `ModuleDto`, or `409 Conflict` if order `1` is taken.

### Example: create a lesson

```http
POST /api/content/modules/{moduleId}/lessons
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "title": "Welcome",
  "content": "Lesson body...",
  "videoUrl": "https://...",
  "order": 1,
  "durationMinutes": 8
}
```

New lessons start as `Draft`; call `/lessons/{lessonId}/publish` to make them visible to
students.

### Example: attach a resource

```http
POST /api/content/lessons/{lessonId}/resources
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "fileId": "00000000-0000-0000-0000-000000000000",
  "resourceType": "Document"
}
```

Returns `200 OK` if the file exists in the File Service and the lesson is found;
otherwise `400 Bad Request` ("File or lesson not found.").

### Example: internal has-content check

```http
GET /api/internal/content/courses/42/has-content
X-Api-Key: <ContentService key>
```

```json
{ "courseId": 42, "hasContent": true }
```

---

## Pagination

All list endpoints accept `pageNumber` and `pageSize` query parameters and return a
`PaginatedResult<T>`:

```http
GET /api/content/courses/42/modules?pageNumber=1&pageSize=10
```

```json
{
  "items": [ /* ModuleDto[] */ ],
  "totalCount": 23,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

`PageNumber` defaults to 1; `PageSize` defaults to 10 and is clamped to **1–100** to
prevent abuse.

---

## File Service integration

`FileServiceClient` (registered as a typed `HttpClient`) talks to the File Service using
the base URL in `ServiceUrls:FileService` and sends the `InternalApiKeys:FileService` key
in the `X-Api-Key` header on every call. It exposes:

- `FileExistsAsync(fileId)` → `GET api/internal/files/{fileId}/exists` — used before
  attaching a resource.
- `GetFileMetadataAsync(fileId)` → `GET api/internal/files/{fileId}/metadata` — available
  but not currently called by `ContentService`.

---

## Testing

```bash
# from the Lms.ContentService folder
dotnet test
```

- **Lms.ContentService.UnitTests** — domain rules for `CourseModule` and `Lesson`
  (ordering, lifecycle, validation).
- **Lms.ContentService.IntegrationTests** — the three controllers plus the API-key
  middleware via `Microsoft.AspNetCore.Mvc.Testing`, against an in-memory EF Core
  provider (`CustomWebApplicationFactory`).

---

## Database

EF Core targets SQL Server. All tables live under the `Content` schema
(`Content.Modules`, `Content.Lessons`, `Content.LessonResources`) so the service can share
a physical database with other LMS services without name collisions.

- Unique indexes: `(CourseId, Order)` on modules, `(ModuleId, Order)` on lessons,
  `(LessonId, FileId)` on resources; plus a non-unique index on `Module.CourseId`.
- `Lesson.Status` is persisted as its string name via a value converter.
- Foreign keys cascade-delete (module → lessons → resources).

Migrations are applied automatically on startup on a relational provider.

---


### Known issues / cleanup TODOs

- **Role claim type mismatch (verify before relying on instructor endpoints).**
  `Program.cs` validates with `RoleClaimType = ClaimTypes.Role`, but the Auth service
  issues roles in a short `"role"` claim. As wired, `[Authorize(Roles = "Instructor,Admin")]`
  and `User.IsInRole(...)` will not match real Auth tokens. To stay consistent with Auth,
  set `RoleClaimType = "role"` (Auth's `Program.cs` does exactly this).
- **Folder / file naming typos:** `Infrastructure/Clientes` (the namespace inside is
  `...Infrastructure.Clients`, so the folder name doesn't match the namespace),
  `Domain/Interfaces/IContentRepository.cs.cs` (double extension), and the
  `UnitTests/Domian` test folder. Rename for tidiness.
- `IFileServiceClient.GetFileMetadataAsync` is defined and implemented but never called.
- `InternalContentController.GetLessonInfo` returns the full `LessonDto` (same shape as
  the student endpoint); if "info" is meant to be a lighter projection, give it its own DTO.
- No live Azure deployment URL is recorded here yet (unlike the Auth service README) —
  add it once the service is published.
