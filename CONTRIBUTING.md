# Contributing

Thanks for taking an interest in this sample.

## Local workflow

1. Copy `.env.example` to `.env` and add local-only credentials.
2. Start PostgreSQL and Redis with `docker compose up -d`.
3. Make a focused change.
4. Run `dotnet format` when available, then `dotnet test GeminiWeatherApi.sln`.
5. Keep secrets, `.env`, database files, and Docker volumes out of commits.

## Pull requests

- Explain the behavior change and how it was tested.
- Keep API responses backward-compatible when practical.
- Add or update tests for parsing, authentication, caching, and persistence behavior.
- Do not include real API keys, user data, or production connection strings.
- Update the README when setup or endpoint behavior changes.
