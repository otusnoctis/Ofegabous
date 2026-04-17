# Configuration

The template's derivable configuration is centralized in `Directory.Build.props`.

## Main properties

- `TemplateAppDisplayName`
- `TemplateAppDescription`
- `TemplateAppId`
- `TemplateAppDisplayVersion`
- `TemplateAppVersion`
- `TemplateRepositoryUrl`
- `TemplateAuthors`
- `TemplatePackageId`
- `TemplateDataDirectoryName`
- `TemplatePersistenceFileName`
- `TemplateDocumentationDirectoryName`
- `TemplateBrandColor`
- `VelopackVersion`

## What these properties drive

### Identity and packaging

- visible window title
- application id
- visible version
- packaging version
- assembly metadata

### Template runtime

- data folder name
- persistence file name
- documentation folder name
- update repository

### Basic branding

- main color for the icon, splash screen, and visual theme

## Why it is centralized

If a derived app needs to be renamed or moved to another repository, the goal is for those changes to come from a single source rather than duplicated strings across many layers of the project.

## Recommendation for derived apps

Before changing pages or services, update `Directory.Build.props` first. That is the correct place to establish the identity of the derived product.
