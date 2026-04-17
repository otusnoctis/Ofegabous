# Release Process

Template releases are `tag-driven`.

## Flow

1. A semantic tag such as `v0.3.1` is created.
2. GitHub Actions runs the release workflow.
3. `.NET 10` is installed.
4. The `maui` workload is installed.
5. `scripts/pack-app.ps1` publishes and packages the app.
6. The artifacts are uploaded to the GitHub Release.

## Main files

- `scripts/pack-app.ps1`
- `.github/workflows/release.yml`
- `Directory.Build.props`

## Packaging details

The script:

- reads shared values from `Directory.Build.props`
- publishes the app for `win-x64`
- packages the release with Velopack
- generates artifacts ready to upload

## Why tags

This approach reduces complexity and leaves a workflow that is easy to explain:

- the version is defined by the tag
- the release can be reproduced
- the repository keeps a clear history of published versions
