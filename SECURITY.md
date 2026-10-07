# Security Policy

## Supported versions

| Version | Supported |
| --- | --- |
| 1.0.x and later | ✅ |
| 1.0.0-preview.* | ❌ |

The latest minor release of the current major version is supported. An older minor version receives a fix only for a security issue or data loss, on a `release/X.Y` branch; a pre-release is unsupported once a newer pre-release or the stable version exists. For the full policy, see [docs/support-policy.md](docs/support-policy.md).

## Reporting a vulnerability

**Never report a vulnerability in a public issue, discussion or pull request.** Report it privately through GitHub's private vulnerability reporting form:

https://github.com/DigitalAbsurd/TabletopCardEngine/security/advisories/new

When you report, include:

- The affected package or container image and its version
- The database provider (SQLite, PostgreSQL, SQL Server, or InMemory)
- Steps to reproduce, or a proof of concept
- The impact of the vulnerability

Only the project maintainers can read the report, and you will be notified when it is addressed.

## What happens next

1. The maintainers acknowledge your report.
2. The team agrees with you on a fix and a responsible disclosure date.
3. A fix is prepared and the repository maintainers publish a GitHub security advisory with the release that fixes it.

This is a small project handled on a best-effort basis. We aim to respond promptly to vulnerabilities, but we do not promise a specific response time.

## Not vulnerabilities, by design

The following are **intentional architectural decisions** and are not treated as security vulnerabilities:

- **The REST API and the container have no authentication and expose every seat's hole cards.** The API is a trusted, game-master API meant for a trusted network or running behind an authenticating reverse proxy. See [docs/docker.md#security](docs/docker.md#security) and [docs/aspnetcore.md#security](docs/aspnetcore.md#security).
- **The container images are published without additional hardening.** They run a trusted service in a trusted environment. See [ADR-0005 (container host and images)](docs/adr/0005-container-host-and-images.md).
- **The demo site runs entirely in the visitor's browser and stores nothing on a server.** See [ADR-0004 (ASP.NET Core integration package)](docs/adr/0004-aspnetcore-integration-package.md).
