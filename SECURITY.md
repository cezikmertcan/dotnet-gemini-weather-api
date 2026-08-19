# Security policy

This repository is an educational integration sample. It includes defensive defaults for a public demo, but it is not a complete production security baseline.

## Supported versions

| Version | Support |
| --- | --- |
| `main` | Security fixes are considered |
| Unreleased commits | Best effort only |

There are currently no published release branches or security support guarantees.

## Reporting a vulnerability

Please do not open a public issue containing credentials, personal data, or an exploitable vulnerability. Once the repository is published, use GitHub’s private security advisory workflow or contact the repository owner privately.

Include the affected commit or route, reproduction steps, impact, and a suggested mitigation when it is safe to do so. Do not include live API keys, JWTs, database credentials, or user data.

## Local safety checklist

- Keep `.env` out of Git.
- Use a unique JWT signing key for each environment.
- Use a secret manager for deployed Gemini credentials and database passwords.
- Put the API behind HTTPS and a trusted reverse proxy.
- Keep PostgreSQL and Redis on a private network; never expose their ports publicly.
- Set Gemini quotas or spend limits before exposing the AI endpoint.
- Rotate any credential that has appeared in logs, screenshots, issues, or Git history.
- Add account lockout, email verification, audit retention, and abuse monitoring before a public production deployment.
- Treat Gemini output as untrusted text and validate it before presenting it as advice.

The sample’s registration and login endpoints are intentionally open, JWTs are stateless, and the included rate limiter is a lightweight per-observed-IP safeguard. These choices are suitable for learning and a controlled demo, not for handling sensitive production accounts without additional controls.
