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
- Instead of JWT, we use a database-backed custom token system for strict auditing.
- `CustomAuthMiddleware` intercepts requests, extracts the token from the `Authorization` header, and calls `IAuthService.ValidateTokenAsync` against the database.
- It bypasses validation for login routes.
- Once validated, it injects Claims into `HttpContext.User`.

## 5. Adding Future ERP Modules (HRMS, Payroll, Inventory)
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

## 6. Exception Handling and Responses
- **NEVER** throw raw exceptions to the client.
- **ALWAYS** use `ApiResponse<T>`.
- Use custom exceptions (`AppException`, `UnauthorizedException`) in Application/Domain layers. The `ExceptionMiddleware` will catch these and automatically format them into a standard HTTP 400 or 401 `ApiResponse`.

## 7. Best Practices
- **Dependency Injection**: Always inject interfaces (`IUserService`), never concrete classes.
- **Async All The Way**: Use `async`/`await` for all I/O operations (Database, Network, File).
- **Validation**: Validate DTOs in the Application layer (consider using FluentValidation).
