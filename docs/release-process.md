# Release process: branching, tags and cutting a release

## What this document is

This document describes how releases are cut and versioned in this repository. It covers the branching model (GitHub Flow on `main`), how release branches are created and maintained for patch releases, how versions and tags are assigned, and the steps to cut a release. Read [docs/versioning.md](versioning.md) for the versioning policy and breaking change rules, and [docs/task-run.md](task-run.md) for how a single change reaches `main`.

## Branching: GitHub Flow on `main`

- `main` is the only long-lived branch and is always releasable. No develop branch and no long-lived feature branches.
- Every change is a short-lived branch created from `main`, named as described in [docs/task-run.md](task-run.md) (Git and pull request protocol section).
- Changes are merged back to `main` by squash through a pull request. The pull request title becomes the commit message on `main`.

## Release branches

- `release/X.Y` is created only when a fix is needed for an already released minor `X.Y` after `main` has moved on to `X.(Y+1)` or later. It is created from the latest `vX.Y.Z` tag.
- The fix lands on `main` first, then it is cherry-picked onto `release/X.Y` using `git cherry-pick -x`.
- The patch is tagged `vX.Y.(Z+1)` on that branch.
- A release branch is never merged back to `main`.
- It is deleted once that minor is no longer supported, as documented in `docs/support-policy.md`.

## Tags and versions

- Tags are annotated and start with `v`: `v1.0.0-preview.1`, `v1.0.0-rc.1`, `v1.0.0`.
- A tag is never moved or reused. A bad release gets a new tag.
- The ladder for 1.0 is `preview.N`, then `rc.N`, then the stable `1.0.0`.
- A tag on a commit is what gives every package and image that version through MinVer, as documented in [docs/task-run.md](task-run.md) (fact 30).

## Cutting a release

1. `main` is green on the four local gates (`dotnet build`, `dotnet format --verify-no-changes`, `dotnet test`, `dotnet pack`), plus the browser gate and the provider matrix.
2. Update `CHANGELOG.md` in a pull request titled `chore(release): vX.Y.Z[-pre]` (a sibling issue adds the command).
3. Once it is merged, tag that merge commit: `git tag -a vX.Y.Z -m "vX.Y.Z"` and `git push origin vX.Y.Z`.
4. The release workflow does the rest (a sibling issue documents it under "The release workflow").

## Protecting `main`

Branch protection is not enforced while the repository is private (GitHub Pro is required). Protecting `main` with required status checks and code reviews is planned for the milestone `v1.0 · NuGet packages & public launch`.
