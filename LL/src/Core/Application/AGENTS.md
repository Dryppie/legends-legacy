# Application layer instructions

## Scope

- This folder contains application contracts, use cases, MediatR requests, DTOs, mappings, and service interfaces.
- Keep controllers, EF Core implementation details, and presentation concerns outside this layer.
- Preserve dependency direction: Application may depend on Domain and shared primitives, but not API, Infrastructure, or Presentation.

## Commands and queries

- Follow the existing CQRS/MediatR folder pattern for new functionality.
- Put each command in its own folder and file:
  `UseCases/<Feature>/Commands/<CommandName>/<CommandName>Command.cs`.
- Put each query in its own folder and file:
  `UseCases/<Feature>/Queries/<QueryName>/<QueryName>Query.cs`.
- Do not group many unrelated commands and queries into a single aggregate request file.
- Keep the request record and its handler together in the same file unless an existing feature uses a different local pattern.
- Commands that mutate state must implement `ICommand<TResponse>` so they use the transaction-backed command pipeline.
- Queries that only read state should implement `IQuery<TResponse>`.
- Do not use plain `IRequest<TResponse>` for ordinary application commands or queries unless the surrounding feature already requires it.
- Keep DTO files focused. Prefer one DTO per file instead of aggregate DTO files that collect a whole feature's contracts.
- Keep AutoMapper profiles focused by feature/use-case concern instead of one large aggregate mapping profile.

## Handler behavior

- Keep handlers thin: validate request-level concerns, call the appropriate application service, and wrap responses consistently.
- Put business rules in services/domain logic rather than controllers.
- Keep API controllers as transport adapters that call `Mediator.Send(...)`.
- Prefer existing `Response<T>` and DTO patterns for user-facing success/failure results.
- Service-layer interfaces must not return DTOs. Services should return domain models or explicit domain/service result objects.
- Application handlers are responsible for mapping domain/service results to DTOs.
- Use `IMapper` for domain-to-DTO mapping instead of manually constructing DTOs inside services.
- Services must not depend on `IDbContext` or EF Core APIs directly. Put database reads, includes, adds, removes, and query details behind repositories.
- Service methods should call repository interfaces for persistence and keep their own code focused on orchestration and business decisions.

## Essence system notes

- Essence write operations must remain command requests using `ICommand<TResponse>`.
- Essence read operations should be query requests using `IQuery<TResponse>`.
- Essence attunement slots are derived from character level through the slot unlock service, not stored as empty slot rows.
- Fetch character level together with related essence loadout data when the level is needed for read DTOs; do not add an extra database call just to calculate unlocked slots.
