# Service layer instructions

- Services must not depend on `IDbContext`, `DbContext`, `DbSet`, or EF Core query APIs directly.
- Services should get persisted data through repository interfaces.
- Repositories own database reads, includes, query filters, counts, adds, removes, and other persistence details.
- Keep services focused on orchestration, validation, business decisions, and coordinating domain operations.
- Do not return DTOs from services. Return domain models or explicit domain/service result objects and let Application handlers map to DTOs.
- Do not introduce direct database access into a service to make a quick feature work; add or extend a repository method instead.
