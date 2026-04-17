# Deriving An App

A derived app should reuse the template baseline and replace only what is product-specific.

## Minimum steps

1. Update `Directory.Build.props`.
2. Adjust branding, id, and release repository.
3. Rewrite `Home` to describe the real product.
4. Replace `Persistence` with the first real feature or adapt its JSON schema.
5. Keep `Documentation` and rewrite its content for the derived product.
6. Keep `Settings` as the diagnostics and update surface.
7. Update icons, splash, and assets.

## What usually stays

- shell MAUI Blazor Hybrid
- service structure
- documentation system
- Velopack update flow
- tag-driven release automation

## What usually changes

- product naming
- business pages
- data model
- documentation content
- final visual tone

## Practical rule

If a derived app needs to rebuild too many basic runtime pieces, that change should probably happen in the template first so it benefits all future derivatives.
