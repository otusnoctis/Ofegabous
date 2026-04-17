# Persistence

The template uses local JSON-based persistence as a functional example and as a starting point for local-first apps.

## Example file

The current example uses:

- `data/persistence.json`

The `Persistence` page lets you:

- load the document
- edit its content
- save it again
- inspect the effective path of the file used by the app

## Responsible service

Persistence is concentrated in `PersistenceStore`.

Its responsibilities are:

- read the document from disk
- write the document as JSON
- create the data folder if it does not exist
- use a bundled document as seed content if the persistent file does not exist yet

## Why JSON

The choice is intentional:

- it is easy to inspect
- it is easy to migrate
- it does not force a database too early
- it works well for small and medium tools

## When to replace it

A derived app can keep JSON if:

- the data volume is modest
- the model is simple
- concurrent access is minimal

It can be replaced with SQLite or another strategy if:

- complex queries are needed
- there are multiple related collections
- the data volume grows
- transactions or indexes are needed

## Template rule

The example page should be real, but it should not impose a definitive persistence solution on every derived app.
