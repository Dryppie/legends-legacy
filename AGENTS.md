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

- `LL/src/Presentation/ll` is an npm-only project managed by `package-lock.json`. Never invoke pnpm, pnpx, Yarn, Bun, or their Corepack variants in this repository. If npm fails, diagnose and repair npm instead of switching package managers.
- Keep package-manager caches outside the repository. In sandboxed Windows sessions, use a directory beneath `$env:TEMP`; never use `.artifacts`, `.tmp`, or another checkout path for an npm cache.
- Identify the target service before editing files.
- Keep changes scoped to the requested feature. Do not refactor unrelated code.
- Follow existing patterns before introducing new abstractions.
- Preserve dependency direction. Core must not depend on API, Infrastructure, or Presentation.
- Prefer maintainable, data-driven solutions suitable for a solo developer.
- Do not add placeholder implementations unless explicitly requested.
- Do not commit secrets, tokens, generated credentials, or environment-specific values.
- Do not deploy services or apply changes to external environments.
- EF Core migrations may be generated when requested, but must not be applied to shared or production databases.

## Test execution

- Run backend tests through `build/run-tests.ps1`. It always runs the fast correctness suite and decides on the exhaustive balance suite for you.
- The balance suite is every test tagged `[Trait("Category", "BalanceFull")]`, declared with `[BalanceFact]` / `[BalanceTheory]`.
- Locally the balance suite runs only when its composite identity differs from `.artifacts/balance-suite.version`. The identity covers versioned equipment, combat, pacing, raid, cooperative-roster, and Tower rules plus hashes of ability, raid-boss, and Tower-floor data. A successful balance run rewrites that stamp, so the suite stays quiet until a relevant input moves again.
- Force it with `build/run-tests.ps1 -IncludeBalance` or `LL_RUN_BALANCE=1`; suppress it with `-SkipBalance` or `LL_RUN_BALANCE=0`.
- In CI the balance suite runs only for pushes to `releases/**` and for manual dispatches. Pull requests and pushes to `main` run the fast suite only.

## Completion requirements

Before finishing:

1. Summarize the changed files.
2. Explain important design decisions.
3. Run the relevant verification commands.
4. Report any commands that could not be run.
5. Call out migrations, configuration changes, and deployment implications.
