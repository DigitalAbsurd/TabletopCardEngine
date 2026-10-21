## Summary

**What** — describe the change briefly (one sentence if possible)

**Why** — link the issue or explain the reason for this change

Closes #

## Checklist

Before merging, confirm:

- [ ] **Title** is a [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) (`<type>(<scope>)?: <summary>`) — it becomes the squash commit and the CHANGELOG entry
- [ ] **Four gates** pass locally (from the repository root):
  - [ ] `dotnet build` — passed
  - [ ] `dotnet format TabletopCardEngine.sln --verify-no-changes` — passed
  - [ ] `dotnet test` — passed
  - [ ] `dotnet pack TabletopCardEngine.Packages.slnf -c Release` — passed

### When this change needs additional checks

- [ ] **Browser layout gate** (required for layout or demo changes; preferred for all others):
  ```
  TCE_TEST_BROWSER_GATE=1 dotnet test tests/TabletopCardEngine.Demo.BrowserTests
  ```
  — summary: [paste result here]

- [ ] **Provider matrix** (for Holdem/Data/, persistence packages, persistence tests, server, or scripts/add-migration.ps1):
  ```
  TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Tests -c Release -- --filter "FullyQualifiedName~Persistence|FullyQualifiedName~MultiHandSession|FullyQualifiedName~ConcurrencyTests"
  TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Server.Tests -c Release -- --filter "FullyQualifiedName~ServerEndToEnd"
  ```
  — summary: [paste result here]

- [ ] **Container image tests** (for Dockerfile, .dockerignore, server, Directory.Build.props, or .editorconfig):
  ```
  TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Server.Tests -c Release -- --filter "FullyQualifiedName~ContainerImage"
  ```
  — summary: [paste result here]

- [ ] **Documentation build** (for docs/, mkdocs.yml, or .readthedocs.yaml):
  ```
  docker run --rm -v "${PWD}:/docs" squidfunk/mkdocs-material:9.7.7 build --strict --site-dir /tmp/site
  git grep -nE "\]\([^):]*(task-run|tasks-planning)\.md" -- 'docs/*.md' 'docs/**/*.md' ':!docs/task-run.md' ':!docs/tasks-planning.md'
  ```

### When this change affects the public API

- [ ] **Public API** — reviewed against the published 1.0.0 baseline on nuget.org; [see docs/public-api.md](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/public-api.md)

- [ ] **Observability** — new events, tags, metrics added to [docs/observability.md](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/observability.md)

- [ ] **Third-party packages** — updated [THIRD-PARTY-NOTICES.md](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/THIRD-PARTY-NOTICES.md) by running `pwsh ./scripts/audit-licenses.ps1`

### When this change touches the database schema

- [ ] **Migrations** — created for every supported provider; see [docs/persistence.md](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/persistence.md)

---

**No workflow runs on pull requests.** The local gates above are the only checks. Merge right after opening this pull request.

See [CONTRIBUTING.md](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/CONTRIBUTING.md) for the full contribution guide.
