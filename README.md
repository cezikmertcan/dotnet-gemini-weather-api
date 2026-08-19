# Gemini Weather API

A production-minded .NET 10 Web API sample that combines live weather measurements with a structured Google Gemini response.

The project is intentionally small and explainable: Open-Meteo remains the source of truth for measurements, Gemini turns those measurements into a constrained answer, Redis reduces repeated upstream calls, and PostgreSQL stores authenticated request history.

## What this demonstrates

- ASP.NET Core Web API targeting .NET 10
- JWT registration and login with PBKDF2 password hashing
- PostgreSQL persistence through Entity Framework Core and Npgsql
- Redis caching with a ten-minute weather-brief cache
- Google Gemini REST integration with JSON response-schema enforcement
- Open-Meteo geocoding and current-weather data
- Per-user request history
- RFC 7807-style Problem Details errors
- IP-based rate limiting for authentication and AI requests
- Liveness and database-readiness health checks
- OpenAPI JSON, Swagger UI, and an English interactive landing page
- Cross-platform local development with Docker Compose
- Multi-stage, non-root Docker image
- Unit tests and a CI pipeline that builds the container image

## Request flow

```text
POST /api/weather/brief
        │
        ├─ authenticate the user with JWT
        ├─ check the user-scoped Redis cache
        ├─ geocode the city with Open-Meteo
        ├─ fetch current measurements from Open-Meteo
        ├─ ask Gemini for a constrained JSON answer
        ├─ cache the typed response for ten minutes
        ├─ persist the request and result in PostgreSQL
        └─ return a stable API response
```

Redis is treated as an optimization rather than a source of truth. If Redis is temporarily unavailable, a live request can still complete and the API logs the cache failure.

## API surface

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Public | Create a user and return a JWT |
| `POST` | `/api/auth/login` | Public | Authenticate a user and return a JWT |
| `POST` | `/api/weather/brief` | Bearer JWT | Generate a structured weather brief |
| `GET` | `/api/history?limit=20` | Bearer JWT | Read the current user’s latest requests |
| `GET` | `/health/live` | Public | Confirm that the process is running |
| `GET` | `/health/ready` | Public | Check PostgreSQL readiness |
| `GET` | `/swagger` | Public | Explore and execute the API interactively |
| `GET` | `/openapi/v1.json` | Public | Download the OpenAPI document |

The interactive root page at `/` explains the architecture and includes a small same-origin client for registration, login, weather requests, and history.

## Requirements

- .NET 10 SDK
- Docker Desktop with Docker Compose
- A Gemini API key for live analysis

The application runs on macOS, Windows, and Linux. PostgreSQL and Redis run in Docker so local infrastructure is consistent across operating systems.

## Quick start

1. Copy the environment template:

```bash
cp .env.example .env
```

On Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

2. Put your Gemini key in `.env`:

```text
GEMINI_API_KEY=your-real-key
```

3. Start PostgreSQL and Redis:

```bash
docker compose up -d
```

4. Start the API:

```bash
dotnet run --project src/GeminiWeatherApi --urls http://localhost:5050
```

The first start applies the checked-in EF Core migration automatically.

Open:

- Landing page: <http://localhost:5050/>
- Swagger UI: <http://localhost:5050/swagger>
- OpenAPI JSON: <http://localhost:5050/openapi/v1.json>
- Liveness: <http://localhost:5050/health/live>
- Readiness: <http://localhost:5050/health/ready>

In Swagger, register or log in first, copy the returned `accessToken`, choose **Authorize**, and paste the token. Protected weather and history routes can then be executed with **Try it out**.

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

Read the authenticated user’s history:

```bash
curl "http://localhost:5050/api/history?limit=20" \
  -H "Authorization: Bearer $TOKEN"
```

## Configuration

Environment variables override `appsettings.json`. `.env` is loaded for local development only and is ignored by Git.

| Variable | Required | Purpose |
| --- | --- | --- |
| `GEMINI_API_KEY` | Always | Gemini authentication secret |
| `GEMINI_MODEL` | No | Gemini model name; defaults to `gemini-3.7-flash` |
| `JWT_SIGNING_KEY` | Production | JWT signing key; must contain at least 32 characters |
| `JWT_ISSUER` | No | JWT issuer; defaults to `GeminiWeatherApi` |
| `JWT_AUDIENCE` | No | JWT audience; defaults to `GeminiWeatherApi.Client` |
| `JWT_ACCESS_TOKEN_MINUTES` | No | Token lifetime from 5 to 1440 minutes |
| `DATABASE_CONNECTION` | Production | PostgreSQL/Npgsql connection string |
| `REDIS_CONNECTION` | Production | Redis connection string |
| `POSTGRES_DB` | Local Compose | PostgreSQL database name |
| `POSTGRES_USER` | Local Compose | PostgreSQL username |
| `POSTGRES_PASSWORD` | Local Compose | Local PostgreSQL password |

Outside the `Development` environment, the application fails fast when `DATABASE_CONNECTION`, `REDIS_CONNECTION`, or `JWT_SIGNING_KEY` is missing. Use a platform secret manager or environment settings for deployed values.

## Docker

Build the production image:

```bash
docker build --tag gemini-weather-api:local .
```

The image listens on port `8080` and runs as a non-root user. It contains only the API; PostgreSQL and Redis should be provided by managed services or separate private containers in a deployment environment.

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for container, managed-database, and platform guidance.

## Data and caching

Every successful weather brief creates an `AiRequestRecord` for the authenticated user. Records contain the topic, city, prompt, model, cache status, response JSON, duration, and timestamp.

Redis keys are scoped by user, city, question, and model. A cache hit still creates a history record, so history represents user activity rather than only Gemini calls.

The ten-minute cache is a demo policy. It is not a guarantee that weather data is fresh enough for emergency or safety-critical decisions.

## Reliability and safety behavior

- Gemini output is parsed as JSON and normalized before it is returned.
- Provider response bodies are intentionally omitted from logs to avoid leaking upstream content.
- Open-Meteo and Gemini timeouts are mapped to explicit gateway errors.
- Authentication is limited to 10 observed client-IP requests per minute.
- Weather/Gemini requests are limited to 20 observed client-IP requests per minute.
- Health checks distinguish process liveness from PostgreSQL readiness.
- Production configuration does not silently fall back to local database, Redis, or JWT values.

## Testing and CI

Run the solution tests:

```bash
dotnet test GeminiWeatherApi.sln --configuration Release
```

Build the solution:

```bash
dotnet build GeminiWeatherApi.sln --configuration Release
```

The GitHub Actions workflow restores, builds, tests, and builds the Docker image. Dependabot is configured for NuGet packages and GitHub Actions.

## Local cleanup

Stop local infrastructure:

```bash
docker compose down
```

To remove this project’s local PostgreSQL and Redis volumes as well:

```bash
docker compose down -v
```

This removes only the named volumes declared by this Compose project.

## Public-demo limitations

This is an educational integration sample, not a complete production security baseline. Before handling real users or sensitive data, add account lockout, email verification, refresh-token rotation or revocation, a retention policy, structured audit logging, centralized secret management, distributed rate limiting, backups, and a documented incident process.

The Gemini free tier and upstream API limits are external to this repository. A public deployment should enforce quotas and monitor usage so an exposed endpoint cannot consume the project’s entire allowance.

## Contributing and security

See [CONTRIBUTING.md](CONTRIBUTING.md) for the local workflow and pull-request expectations. See [SECURITY.md](SECURITY.md) for vulnerability reporting guidance.

Do not commit `.env`, production keys, database passwords, JWT signing keys, or personal data. Rotate any credential that has ever been exposed.

## License and third-party services

This project is released under the MIT License; see [LICENSE](LICENSE).

Gemini and Open-Meteo usage notes are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Review their current terms, attribution requirements, model availability, and billing policies before deploying publicly.
