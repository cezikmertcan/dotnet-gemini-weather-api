# Gemini Weather API

A small, production-minded .NET 10 Web API sample that accepts a weather question, fetches live weather data, asks Google Gemini for a structured interpretation, and stores the result for authenticated users.

This repository is intentionally focused: it demonstrates a practical AI integration without pretending that an LLM is the source of truth for live measurements.

## What it demonstrates

- ASP.NET Core Web API targeting .NET 10
- JWT registration and login with PBKDF2 password hashing
- PostgreSQL persistence through Entity Framework Core and Npgsql
- Redis caching with a ten-minute weather brief cache
- Google Gemini REST integration with JSON response schema enforcement
- Open-Meteo geocoding and current weather data
- Per-user request history
- Problem Details error responses
- Fixed-window rate limiting on the AI endpoint
- Health and OpenAPI endpoints
- Dockerfile and Docker Compose for local infrastructure
- Unit tests for upstream JSON parsing and cache-key isolation

## Architecture

`POST /api/weather/brief`
→ authenticate user
→ look up live weather with Open-Meteo
→ build a constrained prompt
→ request JSON from Gemini
→ cache the typed result in Redis
→ persist the request and result in PostgreSQL
→ return a stable API response

The API never stores the Gemini API key in source code. Local secrets belong in `.env`, which is ignored by Git.

## Requirements

- .NET 10 SDK
- Docker Desktop with Docker Compose
- A Gemini API key for live weather analysis

The API itself is cross-platform: macOS, Windows, and Linux are supported by the .NET runtime. PostgreSQL and Redis run in Docker so the local setup is the same on each operating system.

## Quick start

1. Copy the environment template:

```text
cp .env.example .env
```

On Windows PowerShell, use:

```powershell
Copy-Item .env.example .env
```

2. Put your Gemini key in `.env`:

```text
GEMINI_API_KEY=your-real-key
```

3. Start PostgreSQL and Redis:

```text
docker compose up -d
```

4. Start the API:

```text
dotnet run --project src/GeminiWeatherApi --urls http://localhost:5050
```

The first start applies the EF Core database migration automatically.

- Landing page: http://localhost:5050/
- Interactive Swagger UI: http://localhost:5050/swagger

- OpenAPI document: http://localhost:5050/openapi/v1.json
- Liveness: http://localhost:5050/health/live
- Readiness: http://localhost:5050/health/ready

In Swagger UI, call register or login first, copy the returned `accessToken`, click `Authorize`, and paste the token. Then the protected weather and history routes can be tested with **Try it out**.

## API walkthrough

Register a user:

```bash
curl -X POST http://localhost:5050/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"local-password-123","displayName":"Demo User"}'
```

Log in and copy the returned `accessToken`:

```bash
curl -X POST http://localhost:5050/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"local-password-123"}'
```

Request a weather brief:

```bash
TOKEN="paste-access-token-here"

curl -X POST http://localhost:5050/api/weather/brief \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"city":"Istanbul","question":"Do I need a jacket and an umbrella today?"}'
```

Read the user's latest records:

```bash
curl "http://localhost:5050/api/history?limit=20" \
  -H "Authorization: Bearer $TOKEN"
```

## Configuration

| Variable | Purpose | Example |
| --- | --- | --- |
| `GEMINI_API_KEY` | Gemini authentication | local secret |
| `GEMINI_MODEL` | Configurable Gemini model | `gemini-3.7-flash` |
| `JWT_SIGNING_KEY` | Local token signing key, 32+ chars | local secret |
| `DATABASE_CONNECTION` | PostgreSQL connection string | `Host=localhost;Port=5432;...` |
| `REDIS_CONNECTION` | Redis endpoint | `localhost:6379` |

Do not commit `.env`, production keys, database passwords, or JWT signing keys.

## Data and caching

Every successful weather brief creates an `AiRequestRecord` for the authenticated user. The record contains the normalized topic, city, model, cache status, response JSON, duration, and timestamp.

Redis keys are scoped by user, city, question, and model. A cache hit still creates a history record, so the history endpoint represents user activity rather than only upstream Gemini calls.

The current weather cache lasts ten minutes. This is a demo policy, not a guarantee that weather data is fresh enough for emergency decisions.

## Upstream response safety

Gemini is asked for a JSON object with a fixed schema. The response parser:

- extracts the first JSON object if the provider adds surrounding text
- removes Markdown fences
- validates that a summary exists
- limits recommendation and safety-note array sizes
- returns a generic upstream error without logging the provider response body

The prompt tells Gemini to use only the live measurements supplied by Open-Meteo. For safety-critical decisions, use an official weather warning service instead of this demo.

## Local development

Run tests:

```text
dotnet test GeminiWeatherApi.sln
```

Build:

```text
dotnet build GeminiWeatherApi.sln
```

Stop local infrastructure:

```text
docker compose down
```

To remove local PostgreSQL and Redis data as well, use `docker compose down -v`. This deletes only the named volumes for this project.

## Docker image

The included Dockerfile creates a small ASP.NET runtime image:

```text
docker build -t gemini-weather-api .
```

For a containerized API, provide environment variables appropriate for the network where PostgreSQL and Redis are running. The checked-in Compose file intentionally runs only the dependencies so the API can still be debugged directly with the local .NET SDK.

## Before publishing this repository

- Replace every placeholder in `.env.example` with non-secret documentation values only.
- Confirm `.env` is ignored and no key appears in Git history.
- Set a real repository description and topics on GitHub.
- Enable secret scanning and Dependabot alerts.
- Replace development database credentials in any deployed environment.
- Put the API behind HTTPS and a reverse proxy.
- Rotate JWT signing keys using a secret manager.

## License

MIT. See [LICENSE](LICENSE).

Open-Meteo and Gemini usage notes are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
