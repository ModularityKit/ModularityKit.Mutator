# Package Signing

`ModularityKit.Mutator` release packages are signed as part of the standard package publish path.

The repository signs generated `.nupkg` artifacts before they are uploaded or pushed to package feeds.
Unsigned release packages are treated as a validation failure.

## Signing approach

The repository uses `dotnet nuget sign` with:

- a code-signing certificate provided to GitHub Actions as a base64-encoded PFX
- a certificate password stored in GitHub Actions secrets
- an RFC 3161 timestamp server
- `dotnet nuget verify --all` with the expected SHA-256 signer certificate fingerprint

This keeps signing explicit, auditable, and integrated with the existing `pack` and publish workflows.

## Release workflow

The standard package release path is:

1. pack packages in `.github/workflows/publish-artifacts.yml`
2. sign every `.nupkg`
3. verify every signed `.nupkg`
4. upload signed artifacts
5. download and verify signed artifacts again before pushing to NuGet.org or GitHub Packages

The signing step changes package contents only by adding signature metadata.

## Required GitHub secrets

Configure these repository secrets before using the package publish workflows:

- `NUGET_SIGN_CERTIFICATE_BASE64`
- `NUGET_SIGN_CERTIFICATE_PASSWORD`
- `NUGET_SIGN_CERTIFICATE_SHA256_FINGERPRINT`
- `NUGET_SIGN_TIMESTAMP_URL`
- `NUGET_USERNAME`

Notes:

- `NUGET_SIGN_CERTIFICATE_BASE64` should be the PFX file encoded as base64 without line wrapping changes.
- `NUGET_SIGN_CERTIFICATE_SHA256_FINGERPRINT` must be the SHA-256 fingerprint of the signer certificate used for verification.
- `NUGET_SIGN_TIMESTAMP_URL` should point to the timestamp service provided by the certificate issuer or your preferred RFC 3161 service.
- `NUGET_USERNAME` is still required for the existing NuGet Trusted Publishing login step.

## Local developer expectations

Local development does not require access to signing material.

Contributors can still:

- build the solution
- pack projects locally
- run tests and smoke tests

Signing is enforced in the repository release workflows, not for ordinary local development.

If you have access to the signing certificate locally, you can validate package signatures with:

```bash
dotnet nuget verify path/to/package.nupkg --all --certificate-fingerprint <SHA256_FINGERPRINT>
```

To sign a package locally with the same CLI shape used in CI:

```bash
dotnet nuget sign path/to/package.nupkg \
  --certificate-path path/to/certificate.pfx \
  --certificate-password "<PASSWORD>" \
  --timestamper "<RFC3161_TIMESTAMP_URL>" \
  --timestamp-hash-algorithm SHA256 \
  --hash-algorithm SHA256
```

## NuGet.org expectation

For NuGet.org publishing, the signing certificate must also be registered with the owning NuGet.org account or organization before publishing signed packages.
