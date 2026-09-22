# StemCellsPro ERP Architecture Guidelines

This document outlines the architectural decisions, patterns, and best practices used in the StemCellsPro ERP API project. It is intended for developers to understand how to build and scale the system as new modules are added.

## 1. Architectural Style
The project follows **Clean Architecture** (also known as Onion Architecture). The main principles are:
- **Dependency Rule**: Source code dependencies can only point *inwards*. The Domain layer has no dependencies. Application depends on Domain. Infrastructure and API depend on Application.
- **Separation of Concerns**: Core business logic is isolated from UI (API), databases, and external frameworks.

## 2. Project Layers

### StemCellsPro.Domain
- **Content**: Enterprise-wide business rules, Entities (e.g., `User`, `Employee`, `LeaveRequest`), Value Objects, and Domain Interfaces.
- **Rules**: NO external dependencies (no NuGet packages, no Entity Framework/Dapper). Pure C#.

### StemCellsPro.Application
- **Content**: Application-specific business rules (Use Cases). Contains DTOs, Interfaces (e.g., `IGenericRepository`, `IAuthService`), and Services that orchestrate the business flow.
- **Rules**: References only the Domain layer.

### StemCellsPro.Infrastructure
- **Content**: Implementations of interfaces defined in Application. Database access using Dapper, External API calls, File Storage, etc.
- **Rules**: References Application and Domain. Cannot be referenced by Domain or Application. 

### StemCellsPro.Shared
- **Content**: Cross-cutting concerns like custom Exceptions, centralized API Response wrappers, Constants, and Extensions.
- **Rules**: Can be referenced by any layer, but should not reference any other layer within the solution.

### StemCellsPro.Api
- **Content**: Controllers, Middlewares, Dependency Injection setup, `appsettings.json`.
- **Rules**: References Application, Infrastructure, and Shared. Does not contain business logic; merely delegates to the Application layer.

## 3. Database Access (Dapper)
- We use Dapper for high-performance data access.
- `DapperContext` manages `IDbConnection` creation.
- Keep SQL queries in Repositories or use Stored Procedures (as currently done for Auth).
- For basic CRUD operations, consider using `Dapper.Contrib` or writing a reflection-based generic SQL generator in `GenericRepository`.

## 4. Custom Authentication
- Instead of JWT, we use a database-backed opaque token system for strict auditing and server-side revocation.
- `ApiTokenAuthenticationHandler` extracts the bearer token from the `Authorization` header, calls `IAuthService.ValidateTokenAsync` against APICallDB, resolves the tenant AppDB connection string, and stores it in `ITenantService` for the current request.
- Login routes must be marked with `[AllowAnonymous]`.
- Protected endpoints use standard ASP.NET Core `[Authorize]` attributes and authorization policies.

## 5. Generic Form Operations
- Generic form endpoints are appropriate for metadata-driven forms, master data, and simple configurable ERP/BRSR data entry.
- The API must never accept raw SQL from a client. Query operations should use `FormSearchRequest` with `FormName`, paging, sorting, and whitelisted filters.
- The repository must validate `FormName` against `form_defs`, validate requested columns against the real table schema, parameterize values, and enforce a maximum page size.
- Build explicit controllers/services for workflows with business rules, approvals, calculations, reports, document lifecycle, permissions, or cross-module side effects.

## 6. Adding Future ERP Modules (HRMS, Payroll, Inventory)
When building new modules, follow a **Vertical Slice / Modular Approach** within the Clean Architecture boundaries:

### Example: Adding HRMS (Leave Management)
1. **Domain**: Create `LeaveRequest.cs`, `LeaveType.cs` in `StemCellsPro.Domain/Entities/HRMS/`.
2. **Application**:
   - Create `ILeaveRequestService` in `StemCellsPro.Application/Interfaces/HRMS/`.
   - Create `LeaveRequestDto`, `CreateLeaveDto` in `StemCellsPro.Application/DTOs/HRMS/`.
   - Create `LeaveRequestService` implementing the business rules (e.g., checking employee leave balance).
3. **Infrastructure**:
   - Create `LeaveRequestRepository` in `StemCellsPro.Infrastructure/Repositories/HRMS/` executing Dapper queries.
4. **API**:
   - Create `LeaveController` in `StemCellsPro.Api/Controllers/HRMS/`.
   - Register dependencies in `Program.cs` or an Extension method like `services.AddHrmsModule()`.

## 7. Exception Handling and Responses
- **NEVER** throw raw exceptions to the client.
- **ALWAYS** use `ApiResponse<T>`.
- Use custom exceptions (`AppException`, `UnauthorizedException`) in Application/Domain layers. The `ExceptionMiddleware` will catch these and automatically format them into a standard HTTP 400 or 401 `ApiResponse`.

## 8. Best Practices
- **Dependency Injection**: Always inject interfaces (`IUserService`), never concrete classes.
- **Async All The Way**: Use `async`/`await` for all I/O operations (Database, Network, File).
- **Validation**: Validate DTOs in the Application layer (consider using FluentValidation).
