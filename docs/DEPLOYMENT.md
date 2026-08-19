# Deployment guide

The repository ships a multi-stage Docker image that listens on port `8080` and runs as a non-root user. The application container is stateless; PostgreSQL is the source of truth and Redis is an optional performance layer.

## Required production settings

Configure these values through the host platform’s secret/environment configuration. Do not put them in a checked-in file:

```text
ASPNETCORE_ENVIRONMENT=Production
GEMINI_API_KEY=...
GEMINI_MODEL=gemini-3.7-flash
JWT_SIGNING_KEY=at-least-32-random-characters
JWT_ISSUER=GeminiWeatherApi
JWT_AUDIENCE=GeminiWeatherApi.Client
JWT_ACCESS_TOKEN_MINUTES=60
DATABASE_CONNECTION=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require
REDIS_CONNECTION=...
```

`DATABASE_CONNECTION`, `REDIS_CONNECTION`, and `JWT_SIGNING_KEY` are required outside the Development environment. The first successful application start applies pending EF Core migrations.

## Container smoke test

Build the image:

```bash
docker build --tag gemini-weather-api:local .
```

Run it against reachable managed services. Keep the environment file outside the repository:

```bash
docker run --rm \
  --env-file /secure/path/gemini-weather-api.env \
  --publish 8080:8080 \
  gemini-weather-api:local
```

Verify:

```bash
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
```

Do not expose PostgreSQL or Redis ports to the public internet. Place them on a private network or use managed services with TLS and access controls.

## Platform patterns

### Render

Create a Docker Web Service and configure the service port as `8080`. Use a managed PostgreSQL and Redis-compatible service in the same region. Free instances are suitable for a portfolio demo, but sleep on idle and have storage/reliability limitations; review the current provider limits before relying on them.

### Google Cloud Run

Cloud Run can deploy the checked-in container directly and already expects the image to listen on port `8080`. Use a managed PostgreSQL provider and Redis-compatible provider rather than placing state on the container filesystem. Store `GEMINI_API_KEY` and `JWT_SIGNING_KEY` in Secret Manager when moving beyond a demo.

### EC2 or Lightsail

Install Docker on a small Linux instance, copy an environment file outside the repository, and run the image behind HTTPS. Restrict the security group to HTTP/HTTPS and SSH from a trusted IP; never open PostgreSQL or Redis to `0.0.0.0/0`. Configure backups before treating the service as persistent.

## Production checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Store secrets in the host’s secret manager.
- [ ] Use TLS for PostgreSQL and Redis when supported.
- [ ] Configure HTTPS at the platform or reverse proxy.
- [ ] Set a Gemini spend/quota limit and monitor provider usage.
- [ ] Configure database backups and a retention policy.
- [ ] Review the automatic-startup migration policy for your release process.
- [ ] Verify `/health/live` and `/health/ready` from the platform.
- [ ] Confirm that logs do not contain tokens, API keys, or personal data.
