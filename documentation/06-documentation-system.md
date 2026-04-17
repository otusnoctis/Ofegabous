# Documentation System

The template includes a `Documentation` section inside the app that renders markdown from the root `documentation` folder.

## Goal

Documentation should work well in two contexts at the same time:

1. From the filesystem, as regular markdown.
2. Inside the app, as integrated navigable help.

## How it works

- Markdown files live in `documentation`.
- `DocumentationService` scans the folder and builds the catalog.
- Each document is identified by its relative path.
- The app displays the content in the `Documentation` page.
- The right-side navigator lists the available pages.

## Ordering

Documents are ordered by the numeric prefix in the file name, for example:

- `01-overview.md`
- `02-project-structure.md`

This makes it possible to control the order without relying on an additional manifest file.

## Markdown inside the app

The template uses a simple markdown renderer implemented in project code so it does not depend on external packages.

It supports what the current documentation needs:

- headings
- paragraphs
- listas
- blockquotes
- code fences
- inline code
- enlaces
- basic emphasis

## Internal links

Relative links between `.md` files remain useful outside the app.

Inside the app, those same links are resolved to internal routes such as:

`/documentation?doc=...`

## What to document here

The `documentation` folder should contain the living documentation for the template:

- architecture
- runtime
- persistence
- updates
- releases
- derivation guides

The root `README` should be a concise entry point, not the only place where the project's knowledge lives.
