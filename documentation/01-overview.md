# Overview

`Ofegabous` is a base template for local Windows tools built with `.NET MAUI` + `BlazorWebView` and distributed with `Velopack` + `GitHub Releases`.

It is not meant to be a closed business application. Its purpose is to provide a reusable base for building desktop tools with:

- a stable application shell
- simple side navigation
- JSON-based local persistence
- documentation integrated into the app itself
- automatic updates from GitHub Releases
- centralized configuration that is easy to derive from

## Template goal

A derived app should be able to start from here by changing only what is truly product-specific:

- visible app name
- application identifier
- branding
- release repository URL
- authors
- concrete pages and features
- local data model structure

The baseline that should remain stable is:

- Windows-only
- MAUI Blazor Hybrid
- services separated by responsibility
- tag-driven releases
- centralized content and configuration

## Current stack

- `.NET 10`
- `.NET MAUI`
- `BlazorWebView`
- `Bootstrap`
- `Bootstrap Icons`
- `Velopack`
- `GitHub Actions`
- `GitHub Releases`

## Where to continue

- [Project Structure](./02-project-structure.md)
- [Runtime Model](./03-runtime-model.md)
- [Configuration](./04-configuration.md)
- [Persistence](./05-persistence.md)
- [Documentation System](./06-documentation-system.md)
- [Update System](./07-update-system.md)
- [Release Process](./08-release-process.md)
- [Deriving An App](./09-deriving-an-app.md)
