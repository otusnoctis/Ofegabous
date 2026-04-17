# Project Structure

The current solution contains a single MAUI project named `App`, and organizes the template into several well-separated layers.

## Repository layout

```text
.
|-- .github/workflows/release.yml
|-- documentation/
|-- data/
|-- scripts/
|-- App/
|   |-- Components/
|   |   |-- Layout/
|   |   |-- Pages/
|   |-- Services/
|   |-- Platforms/
|   |-- Resources/
|   |-- wwwroot/
|-- Directory.Build.props
|-- README.md
```

## Folder responsibilities

### `App/Components/Layout`

Contains the main shell:

- `MainLayout`
- side menu
- shared page structure

### `App/Components/Pages`

Contains the template's base pages:

- `Home`
- `Persistence`
- `Documentation`
- `Settings`

### `App/Services`

Contains the non-visual logic:

- derivable template metadata
- environment resolution
- local persistence
- documentation loaded from the filesystem
- updates and startup state

### `documentation`

Contains the source markdown documentation. These files:

- can be opened directly from the filesystem
- are rendered inside the app in the `Documentation` section
- form the canonical documentation for the template

### `data`

Contains example data used by the template in development and as seed content for published builds.

### `.github/workflows` y `scripts`

Contain the release automation:

- packaging
- tag-based versioning
- artifact upload

## Structural principles

- Pages should not contain disk access or update logic.
- Derivable values should not be scattered across the project.
- Documentation should live outside the visual project, in a reusable root folder.
- Functional examples should be simple but real.
