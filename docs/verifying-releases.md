# Verifying releases

This guide explains how to verify the authenticity and integrity of TabletopCardEngine releases.

## Release artifacts

Every release publishes:

- **Release tag (`vX.Y.Z`)**: an annotated, signed Git tag on `main` or a `release/X.Y` branch
- **GitHub Release**: created with notes generated from the commit history
- **NuGet packages**: on nuget.org as `.nupkg` and `.snupkg` files
- **Container images**: on Docker Hub (`digitalabsurd/tabletopcardengine`)

Each artifact carries a build provenance attestation created by GitHub during the release workflow, which you can verify using `gh`.

## Verifying the release tag signature

Every release tag is signed with an SSH key from a YubiKey (see [docs/release-process.md](release-process.md#signing-a-release-tag) for the signing policy). Verify the signature with:

```bash
git verify-tag vX.Y.Z
```

Your local `gpg.ssh.allowedSignersFile` must be set to point to the allowed signers:

```bash
git -c gpg.ssh.allowedSignersFile=.github/allowed_signers verify-tag vX.Y.Z
```

Or check the public keys on [GitHub](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/.github/allowed_signers).

## Verifying the GitHub Release

Verify that the release was created by a signed tag and published without modification:

```bash
gh release verify vX.Y.Z --repo DigitalAbsurd/TabletopCardEngine
```

## Verifying a NuGet package

Download the `.nupkg` from [nuget.org](https://www.nuget.org/packages/TabletopCardEngine/) and verify its build provenance:

1. Extract the DLL from the package:
   ```bash
   unzip -j TabletopCardEngine.X.Y.Z.nupkg 'lib/net10.0/TabletopCardEngine.dll'
   ```

2. Verify the DLL's attestation:
   ```bash
   gh attestation verify TabletopCardEngine.dll --repo DigitalAbsurd/TabletopCardEngine
   ```

The `.nupkg` file itself will have different bytes from what CI built because nuget.org adds a repository signature (see [ADR-0010](adr/0010-release-signing.md)). The DLL inside is what the attestation covers.

To check the repository signature on the package itself:
```bash
dotnet nuget verify --all TabletopCardEngine.X.Y.Z.nupkg
```

## Verifying a container image

Verify the build provenance of a pushed image:

```bash
gh attestation verify oci://docker.io/digitalabsurd/tabletopcardengine:X.Y.Z-sqlite --repo DigitalAbsurd/TabletopCardEngine
```

Supported tags are `X.Y.Z-sqlite`, `X.Y.Z-postgres`, and `X.Y.Z-mssql`. For stable releases (without a prerelease suffix like `-preview` or `-rc`), stable tags `sqlite`, `postgres`, and `mssql` also exist and have the same digest.

## Early releases without attestations

Releases made before the repository went public (such as `v1.0.0-preview.1`) carry no build provenance attestation because GitHub Artifact Attestations require a public repository. The tag signature is still valid — verify it with `git verify-tag`.
