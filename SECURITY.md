# Security policy

This repository is an educational integration sample. It is not a complete production security baseline.

## Reporting a vulnerability

Please do not open a public issue containing credentials, personal data, or an exploitable vulnerability. Once the repository is published, use GitHub's private security advisory workflow or contact the repository owner privately.

## Local safety checklist

- Keep `.env` out of Git.
- Use a unique JWT signing key for each environment.
- Use a secret manager for deployed Gemini credentials and database passwords.
- Put the API behind HTTPS and a trusted reverse proxy.
- Add account lockout, email verification, audit retention, and abuse monitoring before a public production deployment.
- Treat Gemini output as untrusted text and validate it before presenting it as advice.
