# LegendsLegacy repository instructions

## Repository layout

- `LL/` contains the primary game application.
- `LL/src/API/LL` contains the ASP.NET Core API boundary.
- `LL/src/API/AdminDashboard` contains the ASP.NET Core Admin dashboard boundary.
- `LL/src/Core/` contains domain logic and application rules.
- `LL/src/Infrastructure/` contains persistence, external integrations, and implementation details.
- `LL/src/Presentation/ll` contains the Angular frontend.
- `LL/src/Presentation/dashboard` contains an angular dashboard for a few admin CRUDS, like item creations.
- `LL-Chat/` contains the independently deployable chat service.
- Infrastructure-as-code lives in a separate repository and must not be modified from this repository.

## Working rules

- Identify the target service before editing files.
- Keep changes scoped to the requested feature. Do not refactor unrelated code.
- Follow existing patterns before introducing new abstractions.
- Preserve dependency direction. Core must not depend on API, Infrastructure, or Presentation.
- Prefer maintainable, data-driven solutions suitable for a solo developer.
- Do not add placeholder implementations unless explicitly requested.
- Do not commit secrets, tokens, generated credentials, or environment-specific values.
- Do not deploy services or apply changes to external environments.
- EF Core migrations may be generated when requested, but must not be applied to shared or production databases.

## Completion requirements

Before finishing:

1. Summarize the changed files.
2. Explain important design decisions.
3. Run the relevant verification commands.
4. Report any commands that could not be run.
5. Call out migrations, configuration changes, and deployment implications.
