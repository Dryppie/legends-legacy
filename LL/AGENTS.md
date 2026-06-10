# Primary game service instructions

- This service contains the main idle RPG gameplay logic.
- Use CQRS/MediatR patterns where they already exist.
- Domain rules belong in Core, not in controllers or EF Core repositories.
- Controllers should remain thin.
- Persistence-specific logic belongs in Infrastructure.
- Avoid per-ability or per-essence hardcoded scaling logic when shared templates or effects are appropriate.
