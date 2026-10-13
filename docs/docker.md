# Container images

## What this document is

The user guide for the TabletopCardEngine container images. The decisions behind the host, the
container strategy, the provider model and the security stance are recorded in
[ADR-0005](adr/0005-container-host-and-images.md).

## Quick start

Pull the published SQLite image, run it, and test the `/health` endpoint:

```bash
docker run -d -p 8080:8080 -v holdem-data:/data digitalabsurd/tabletopcardengine:sqlite
curl http://localhost:8080/health
```

Then create a game:

```bash
curl -X POST http://localhost:8080/api/holdem/games \
  -H "Content-Type: application/json" \
  -d '{}'
```

Both should return 200. Read [Configuration](#configuration) for other database providers.

## Security

The API returns `GameData` exactly as the engine does, every seat's hole cards included. It is a
trusted, game-master API: the host is for a trusted network or for running behind a reverse proxy
that authenticates and controls access.

There is no API authentication in the container. Any caller that reaches the container can read
every card and place any action. Protect the container accordingly: restrict inbound access to
trusted hosts only, run it behind an authenticating reverse proxy, or both.

## Configuration

The container host is `src/TabletopCardEngine.Server`, an ASP.NET Core application over the
`TabletopCardEngine.AspNetCore` package. It reads its settings the usual ASP.NET Core way, each source
overriding the one before it:

1. `appsettings.json`, which carries logging and `AllowedHosts` only. It holds no connection string
   and no `TabletopCardEngine:Holdem` section, so nothing database-specific is baked into an image.
2. `appsettings.Development.json`, read only when `ASPNETCORE_ENVIRONMENT` is `Development`. It
   points at a local SQLite file, `holdem.db`, and migrates it on start-up.
3. Environment variables, where `__` stands for the `:` of a configuration key
   (`TabletopCardEngine__Holdem__MigrateDatabaseOnStartup` sets `TabletopCardEngine:Holdem:MigrateDatabaseOnStartup`).

The settings it reads so far:

| Setting | Meaning |
|---|---|
| `ConnectionStrings__TabletopCardEngine` | The connection string of the database, required. Examples per provider:<br/>— SQLite: `Data Source=/data/holdem.db`<br/>— PostgreSQL: `Host=db.example.com;Port=5432;Database=holdem;Username=user;Password=pass`<br/>— SQL Server: `Server=db.example.com,1433;Database=holdem;User Id=sa;Password=pass;TrustServerCertificate=True` (see notes below) |
| `TabletopCardEngineServer__Provider` | The database provider: `Sqlite`, `PostgreSql`, or `SqlServer`, required. Case-insensitive. If missing or unknown, the host exits with an error naming the variable. Do not typo this; a silent default would hide a misconfigured image. |
| `TabletopCardEngine__Holdem__*` | Hold'em's host options, bound to `HoldemHostOptions`: `GamePlayersMinCount`, `GamePlayersMaxCount`, `EnableSensitiveCardLogging` and `MigrateDatabaseOnStartup`. The keys, defaults and rules are in [docs/aspnetcore.md](aspnetcore.md). |

To run the host locally, in the `Development` environment on port 5298:

```
dotnet run --project src/TabletopCardEngine.Server
```

It serves the REST API under `/api/holdem` and the health report at `/health`.

### Notes

**SQL Server and TrustServerCertificate.** The `Microsoft.Data.SqlClient` 7 library encrypts connections by default. Most container SQL Server images (including `mcr.microsoft.com/mssql/server:2022-latest`) use self-signed certificates, which fail validation without `TrustServerCertificate=True` in the connection string. Set it there; do not disable SSL altogether. If your SQL Server uses a proper certificate, you can omit this flag.

## OpenAPI

The OpenAPI document is served at `/openapi/v1.json` and describes every endpoint of the REST API. There is no UI; the document can be fed to any OpenAPI tool. See `docs/aspnetcore.md#openapi` for the metadata structure.

## Published images

The repository publishes images to Docker Hub as `digitalabsurd/tabletopcardengine`. The available tags are:

- **`sqlite`, `postgres`, `mssql`** — the latest stable release, one tag per database provider.
- **`<version>-<target>`** — a specific version and target, for example `1.0.0-sqlite` or `1.0.0-preview.1-postgres`. Pre-releases are published with a `-preview` or similar suffix.

The `org.opencontainers.image.version` label on each image carries its version. For production workloads, pin a version tag rather than using `latest` (the release tag without a version prefix), so updates are deliberate.

## Images

The `Dockerfile` at the repository root builds the host in an SDK stage (`builder`) and copies the
published output into an ASP.NET Core runtime stage (`final`, on `mcr.microsoft.com/dotnet/aspnet:10.0`,
Ubuntu 24.04). Three targets build `FROM final` and differ only in their environment defaults
([ADR-0005](adr/0005-container-host-and-images.md), decision 5). Build the images locally from source with:

```
docker build --target sqlite -t tabletopcardengine:sqlite .
docker build --target postgres -t tabletopcardengine:postgres .
docker build --target mssql -t tabletopcardengine:mssql .
```

`sqlite` is the last stage, so `docker build -t tabletopcardengine .` without `--target` builds it too.

Every target sets `ASPNETCORE_HTTP_PORTS=8080` (the port it exposes) and
`TabletopCardEngine__Holdem__MigrateDatabaseOnStartup=true`, and runs as the non-root user `app` (`APP_UID=1654`).
On top of that:

| Target | `TabletopCardEngineServer__Provider` | `ConnectionStrings__TabletopCardEngine` |
|---|---|---|
| `sqlite` | `Sqlite` | `Data Source=/data/holdem.db` |
| `postgres` | `PostgreSql` | not set: pass it with `-e` |
| `mssql` | `SqlServer` | not set: pass it with `-e` |

The `postgres` and `mssql` targets carry no default connection string on purpose: without one the
host refuses to start (decision 3).

```
docker run -d -p 8080:8080 -e "ConnectionStrings__TabletopCardEngine=Host=db;Database=holdem;Username=user;Password=pass" tabletopcardengine:postgres
```

**The `/data` volume.** The `sqlite` target keeps its database in `/data`, a directory owned by `app`
and declared as a volume, because `/app` is not writable by `app`. Mount a named volume there to keep
the database across containers: `docker run -d -p 8080:8080 -v holdem-data:/data tabletopcardengine:sqlite`.
A connection string pointing anywhere `app` cannot write fails at start-up with
`SQLite Error 14: 'unable to open database file'`.

**`Exited (139)` is a configuration error.** A host that fails at start-up, for example with no
connection string or an unknown provider, exits with code 139, so `docker ps -a` shows
`Exited (139)`. It is not a crash of the runtime: `docker logs <container>` shows the exception, which
names the missing or wrong environment variable.

**SQL Server needs `TrustServerCertificate=True`** against a server with a self-signed certificate,
such as the `mcr.microsoft.com/mssql/server:2022-latest` container. Without it the connection fails
with `Certificate failed chain validation`. See [Notes](#notes) above.

**There is no `HEALTHCHECK` instruction** (decision 8). The runtime image has no `curl` or `wget`, and
none is added just for a probe. Probe `/health` from outside the container instead, from the
orchestrator or a load balancer.

## Images built by a release

The `release.yml` workflow builds all three targets for every `v*` tag and publishes them to Docker Hub as `digitalabsurd/tabletopcardengine:<version>-<target>` (for example `digitalabsurd/tabletopcardengine:1.0.0-preview.1-sqlite`), with the version stamped as the `org.opencontainers.image.version` label. For a stable release (no `-preview` or similar suffix), the workflow also publishes with the target-only tag (`digitalabsurd/tabletopcardengine:sqlite`, `digitalabsurd/tabletopcardengine:postgres`, `digitalabsurd/tabletopcardengine:mssql`). A local build with no `--build-arg VERSION=...` is labelled `0.0.0-local`.

## Testing the images

`ContainerImageTests` in `tests/TabletopCardEngine.Server.Tests` is the gate for the images. It builds
the `sqlite`, `postgres` and `mssql` targets from this `Dockerfile` once per run, one after another,
and runs each image on a Docker network of its own. The `postgres` and `mssql` images run next to a
`postgres:16-alpine` or `mcr.microsoft.com/mssql/server:2022-latest` container aliased `db`. Each image
gets only the environment this guide tells a user to set: nothing for `sqlite`, and
`ConnectionStrings__TabletopCardEngine` for the other two. The provider and the start-up migration come from the
image itself. For every target the gate checks that:

- a game can be played from creation to finish over HTTP, the same scenario as the in-process
  `Server_PlaysAGameFromCreationToFinish`;
- `/health` returns 200, with the schema `UpToDate` and the expected EF Core provider;
- `/openapi/v1.json` lists the six `Holdem*` operations;
- the process runs as the non-root user (`id -u` prints `1654`).

It also checks that the `postgres` and `mssql` images without a connection string exit with a non-zero
code and a log naming `ConnectionStrings__TabletopCardEngine`, and that a `sqlite` container keeps a game in its
`/data` volume across a stop and a start.

The gate needs Docker and runs only with `TCE_TEST_CONTAINERS=1`. Without it every test reports
inconclusive. From the repository root:

```
TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Server.Tests -c Release -- --filter "FullyQualifiedName~ContainerImage"
```

The first run builds the images, which takes a few minutes. The test images are tagged
`tabletopcardengine-test:<target>` and are removed when the run ends, together with the containers and
networks it started.

## Running with compose

### PostgreSQL

```bash
docker compose -f docker/compose.postgres.yml up -d --build
```

The `postgres` service uses a named volume `pgdata` on `/var/lib/postgresql/data` to persist the database. The `server` service reads the connection string from `ConnectionStrings__TabletopCardEngine` and the database password from `POSTGRES_PASSWORD` (default: `holdem`).

```bash
docker compose -f docker/compose.postgres.yml down -v
```

### SQL Server

```bash
$env:MSSQL_SA_PASSWORD='Probe_Passw0rd!'
docker compose -f docker/compose.mssql.yml up -d --build
```

The `mssql` service uses a named volume `mssqldata` on `/var/opt/mssql` to persist the database. The `server` service reads the connection string from `ConnectionStrings__TabletopCardEngine` and the database password from `MSSQL_SA_PASSWORD`. The password must be at least 8 characters and contain uppercase, lowercase, digit, and symbol characters.

```bash
docker compose -f docker/compose.mssql.yml down -v
Remove-Item Env:MSSQL_SA_PASSWORD
```

### SQLite

SQLite does not need a separate database container. Run a single command:

```bash
docker run -d -p 8080:8080 -v holdem-data:/data digitalabsurd/tabletopcardengine:sqlite
```

The `sqlite` target keeps its database in `/data`, a named volume `holdem-data`, so the database persists across container restarts.
