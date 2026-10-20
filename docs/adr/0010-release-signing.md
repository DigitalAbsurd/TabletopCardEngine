# ADR-0010: Release tag signing and verification

- **Status:** Accepted
- **Date:** 2026-10-20
- **Issue:** [#471](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/471), milestone
  `v1.0 · NuGet packages & public launch`

## Context

After issues #456–#458, a release is triggered by pushing a `v*` tag. The `release.yml` workflow
calls `create-release.yml`, `deploy-demo.yml`, `publish-nuget.yml` and `publish-docker.yml`
sequentially, or each can be started by hand with `gh workflow run <file> -f tag=vX.Y.Z`. None of
these workflows verify who made the tag, so anyone who can push a tag to the repository can publish
packages and images.

Issue #469 added SSH signing keys to the maintainer's YubiKeys (two hardware-backed `ed25519-sk`
keys, one per device) and committed their public keys in `.github/allowed_signers` on `main`. This
record decides how releases verify that tags are signed by one of these keys, and documents
rejected alternatives.

## Decision

### 1. Tags are signed with SSH FIDO2 keys and CI verifies them

Every `v*` release tag is signed with the maintainer's `ed25519-sk` SSH key via `git tag -s
vX.Y.Z -m "vX.Y.Z"` (which prompts for the PIN and a touch). CI verifies the signature against
`.github/allowed_signers` read from `origin/main`, never from the tag itself (a forged tag would
bring its own key).

All four release stages (`release.yml` and its four called workflows) check the signature before
proceeding. A manual re-run via `gh workflow run` is also checked, and a failed signature check is
fatal and not overridable (`continue-on-error` or any other gate is forbidden).

SSH keys are used instead of X.509 certificates (paid, require a FIPS token, signing on the
maintainer's machine breaks Trusted Publishing) or GPG (more setup for the same result).

### 2. Artifacts carry keyless build provenance attestations

Issue #472 will add GitHub-issued build provenance attestations for each published artifact, issued
by the workflow and signed by GitHub's OIDC provider, without any key in the repository or workflow.

### 3. Published artifacts are immutable

Issue #470 will enforce immutability: a release tag can never be moved or deleted, and a package
version can be published to a registry only once, so accidental or malicious overwrites of a
published artifact are prevented by the registry.

## Rejected alternatives

### NuGet author signing with an X.509 certificate

Requires a paid certificate from a CA, which in turn requires a FIPS 140-2 hardware token for key
storage. The YubiKeys in use are not FIPS-certified. The only workable token would cost thousands
of dollars. More importantly, signing on the maintainer's machine with an X.509 certificate breaks
the CI's Trusted Publishing flow: the agent's id-token (signed by GitHub's OIDC provider) is no
longer the only credential used for access to the NuGet API, which weakens the guarantee.

### cosign with a hardware key

cosign can sign with a YubiKey's PIV slot, but only when the signer runs locally and provides the
key interactively. A CI workflow cannot do this.

### GPG

GPG signing adds more setup (key export, configuration) and does not address the security issue: it
allows the same signature verification in CI, with no improvement over SSH keys and no advantage for
a publicly visible tag.

### Signed commits in the release workflow

The agent runner commits to the repository with the maintainer's configured git user
(`user.name` / `user.email`). A commit signed by the runner would require a signing key in the
repository or in the workflow (and a PIN prompt that a workflow cannot answer), and every commit
would wait for a touch on a security key that is in the maintainer's possession offline. Impractical.

## Consequences

- **The next issues of the milestone implement this record.** This issue adds the verification
  script and wires it into every release stage. Later issues add the immutability and build
  provenance features.
- **The release tag command changes.** Tags must be signed: `git tag -s vX.Y.Z -m "vX.Y.Z"`. The
  maintainer can sign with a backup key via `git -c user.signingkey=<keyid> tag -s vX.Y.Z -m
  "vX.Y.Z"`.
- **A failed verification is not recoverable.** If a tag is unsigned, lightweight, or signed by a
  key not listed in `.github/allowed_signers` on `main`, the release stages fail. The fix is a new
  signed tag for the next version, never a moved or re-signed tag.
- **Documentation changes are required.** `docs/release-process.md` is updated to document the tag
  command and what a failed verification means. `docs/task-run.md` fact 19 adds that every stage
  verifies the tag signature first.
