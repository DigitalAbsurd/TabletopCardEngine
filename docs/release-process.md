# Release process: branching, tags and cutting a release

## What this document is

This document describes how releases are cut and versioned in this repository. It covers the branching model (GitHub Flow on `main`), how release branches are created and maintained for patch releases, how versions and tags are assigned, and the steps to cut a release. Read [docs/versioning.md](versioning.md) for the versioning policy and breaking change rules, and [`docs/task-run.md`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/task-run.md) for how a single change reaches `main`.

## Branching: GitHub Flow on `main`

- `main` is the only long-lived branch and is always releasable. No develop branch and no long-lived feature branches.
- Every change is a short-lived branch created from `main`, named as described in [`docs/task-run.md`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/task-run.md#git-and-pull-request-protocol) (Git and pull request protocol section).
- Changes are merged back to `main` by squash through a pull request. The pull request title becomes the commit message on `main`.

## Release branches

- `release/X.Y` is created only when a fix is needed for an already released minor `X.Y` after `main` has moved on to `X.(Y+1)` or later. It is created from the latest `vX.Y.Z` tag.
- The fix lands on `main` first, then it is cherry-picked onto `release/X.Y` using `git cherry-pick -x`.
- The patch is tagged `vX.Y.(Z+1)` on that branch.
- A release branch is never merged back to `main`.
- It is deleted once that minor is no longer supported, as documented in `docs/support-policy.md`.

## Tags and versions

- Tags are annotated and start with `v`: `v1.0.0-preview.1`, `v1.0.0-rc.1`, `v1.0.0`.
- A tag is never moved or reused. A bad release gets a new tag. The only exception, the deletion of the trial `v1.0.0-preview.1` and `.2` tags before the public launch, is recorded in [ADR-0008](adr/0008-harmonize-the-public-api.md).
- The ladder for 1.0 is `preview.N`, then `rc.N`, then the stable `1.0.0`.
- A tag on a commit is what gives every package and image that version through MinVer, as documented in [`docs/task-run.md`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/task-run.md#engine-facts) (fact 30).

## Cutting a release

1. `main` is green on the four local gates (`dotnet build`, `dotnet format --verify-no-changes`, `dotnet test`, `dotnet pack`), plus the browser gate and the provider matrix.
2. Update `CHANGELOG.md` in a pull request titled `chore(release): vX.Y.Z[-pre]` (the command is in [Changelog](#changelog)).
3. Once it is merged, tag that merge commit: `git tag -a vX.Y.Z -m "vX.Y.Z"` and `git push origin vX.Y.Z`.
4. The release workflow does the rest, as described in [The release workflow](#the-release-workflow).

## The release workflow

- A pushed `v*` tag runs `.github/workflows/release.yml`, the only workflow a tag triggers. Its three verification jobs are `build` (the gates, the browser gate, and `eng/verify-packages.sh` on the packed output: one `.nupkg` per project of `TabletopCardEngine.Packages.slnf`, each ending in the tag's version), `providers` (PostgreSQL and SQL Server) and `docker` (the three images, built and not pushed).
- It then calls four reusable workflows, passing the tag as the input `tag`:

| Stage | Workflow | Waits for | Runs |
|---|---|---|---|
| `create-release` | `create-release.yml` | `build`, `providers`, `docker` | every tag |
| `deploy-demo` | `deploy-demo.yml` | `build`, `providers` | a tag without `-` only; its job also skips itself while the repository is private |
| `publish-nuget` | `publish-nuget.yml` | `create-release` | only when `TCE_PUBLISH` is `enabled` (see [Publishing](#publishing-not-enabled-yet)) |
| `publish-docker` | `publish-docker.yml` | `create-release` | only when `TCE_PUBLISH` is `enabled` |

- `create-release.yml` writes the notes with `git-cliff --latest` and creates the GitHub Release with them. The release carries **no packages**: no `.nupkg`, `.snupkg` or image is attached, and `publish-nuget.yml` packs from the tag itself. If the release already exists, the workflow fails with an error; deleting it is the maintainer's decision.
- A tag that contains `-` (`v1.0.0-preview.1`, `v1.0.0-rc.1`) makes a pre-release.
- Each of the four workflows checks out `refs/tags/<tag>` with full history, because MinVer reads the version from the tag at `HEAD`, and rejects a `tag` that is not `vX.Y.Z` or `vX.Y.Z-suffix` or does not exist.
- A failure in `build`, `providers` or `docker` leaves no release. The fix is a new tag, never a moved one.

### Re-running one stage

A tag is never moved. When one stage fails after the others succeeded (the GitHub Release exists but the demo deploy failed, say, or nuget.org rejected a push), re-run that stage alone for the existing tag:

```bash
gh workflow run create-release.yml -f tag=v1.0.0-preview.1
gh workflow run deploy-demo.yml -f tag=v1.0.0
gh workflow run publish-nuget.yml -f tag=v1.0.0-preview.1
gh workflow run publish-docker.yml -f tag=v1.0.0-preview.1
```

The tag comes from the input, never from the branch the dispatch runs on. A manual run does not repeat the verification jobs of `release.yml`, so run a stage by hand only for a tag whose release run got past them.

### Publishing

`publish-nuget.yml` packs from the tag, checks the output with `eng/verify-packages.sh`, and pushes to nuget.org using NuGet Trusted Publishing. Trusted Publishing uses OIDC (OpenID Connect) to exchange the job's identity token for a short-lived NuGet API key, which is returned by the `NuGet/login@v1` action in the `NUGET_API_KEY` output — no long-lived API key is stored as a secret. The workflow runs only when the repository variable `TCE_PUBLISH` is `enabled`. To enable it:

1. Register `publish-nuget.yml` as a trusted publisher on nuget.org for the account `digitalabsurd`.
2. Register `release.yml` as a separate trusted publisher (the OIDC token names the top-level workflow that started the run, so `release.yml` and `publish-nuget.yml` need separate policies).
3. Set the repository variable `TCE_PUBLISH` to `enabled`.

The `--skip-duplicate` flag in the push command allows a re-run after a partial failure to push only what is missing without failing on already-uploaded packages.

`publish-docker.yml` will push the three images (sqlite, postgres, mssql) to the Docker Hub repository `digitalabsurd/tabletopcardengine` with the `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets, which are already set.

## Changelog

- `CHANGELOG.md` is generated by [git-cliff](https://git-cliff.org) from the commit history, configured in `cliff.toml`. Never edit it by hand: if an entry reads badly, fix `cliff.toml` (or the commit subject, for the future).
- `npx --yes git-cliff@2.14.2 -o CHANGELOG.md` regenerates it. Untagged commits are filed under `## [Unreleased]`.
- `npx --yes git-cliff@2.14.2 --tag vX.Y.Z -o CHANGELOG.md` is the release-preparation command: it files the unreleased commits under the new version.
- `npx --yes git-cliff@2.14.2 --latest --strip all` prints the notes of the latest release, which is what `create-release.yml` uses.
- The tool is always run through `npx` with the version pinned. It is not a NuGet package, so it has no row in `THIRD-PARTY-NOTICES.md`.
- Conventional Commits are grouped by type; a breaking change (`!` or a `BREAKING CHANGE` footer) goes under **Breaking changes**. The prose verbs in `cliff.toml` (`Add`, `Fix`, `Document`, ...) exist for the history from before Conventional Commits became mandatory only. Do not set `filter_unconventional = true`: it would drop that history.

## Protecting `main`

Branch protection is not enforced while the repository is private (GitHub Pro is required). Protecting `main` with required status checks and code reviews is planned for the milestone `v1.0 · NuGet packages & public launch`.
