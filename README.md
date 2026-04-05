# Ofegabous Template

`Ofegabous` es una plantilla base para utilidades locales de Windows construidas con `.NET MAUI` + `BlazorWebView` y distribuidas con `Velopack` + `GitHub Releases`.

La idea no es entregar una aplicacion de negocio cerrada, sino una base reutilizable para crear herramientas de escritorio con:

- shell de aplicacion estable
- navegacion lateral clara
- persistencia local simple basada en JSON
- actualizacion automatica desde GitHub Releases
- configuracion centralizada y facil de derivar

## Objetivo

El repo debe servir como plantilla derivable. Una aplicacion concreta deberia poder nacer desde aqui cambiando:

- nombre visible de la app
- identificador de aplicacion
- branding basico
- URL del repositorio de releases
- autores
- paginas y features concretas
- esquema de datos local

La base que se pretende mantener es:

- Windows-only
- MAUI Blazor Hybrid
- servicios separados por responsabilidad
- release tag-driven
- variables comunes centralizadas

## Stack actual

- `.NET 10`
- `.NET MAUI`
- `BlazorWebView`
- `Bootstrap`
- `Velopack`
- `GitHub Actions`
- `GitHub Releases`

## Arquitectura

La plantilla queda organizada en 4 capas:

1. `App Shell`
   - layout principal
   - menu lateral
   - estructura visual compartida

2. `Pages`
   - `Home`: explica la plantilla, su arquitectura y su modelo de runtime
   - `Persistence`: ejemplo funcional de persistencia local con JSON
   - `Settings`: versionado, estado de instalacion y flujo de actualizaciones

3. `Services`
   - metadata centralizada
   - resolucion del entorno de ejecucion
   - persistencia local
   - estado de arranque Velopack
   - servicio de updates

4. `Release Infrastructure`
   - propiedades compartidas en `Directory.Build.props`
   - script de empaquetado
   - workflow de GitHub Actions

## Estructura del repositorio

```text
.
|-- .github/workflows/release.yml
|-- Directory.Build.props
|-- README.md
|-- data/
|   |-- persistence.json
|-- scripts/
|   |-- pack-app.ps1
|-- App/
|   |-- App.csproj
|   |-- Components/
|   |   |-- Layout/
|   |   |-- Pages/
|   |-- Services/
|   |-- Platforms/
|   |-- Resources/
|   |-- wwwroot/
```

## Modelo de runtime

La app funciona asi:

1. En desarrollo, la app usa la raiz del repo como `root directory`.
2. La persistencia JSON se guarda en `data/persistence.json`.
3. En una instalacion Velopack, la app usa el directorio resuelto desde el runtime instalado.
4. Al arrancar, la app intenta consultar GitHub Releases si esta instalada y no esta en `dev mode`.
5. La pagina `Settings` expone el estado de actualizacion y permite volver a consultar o aplicar la nueva version.

`dev mode` se trata de forma intencional:

- no aplica updates reales
- expone una version `x.x.x-dev`
- deja visible por que las actualizaciones estan deshabilitadas

## Configuracion centralizada

La mayoria de valores derivables estan en [Directory.Build.props](./Directory.Build.props):

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
- `TemplateBrandColor`
- `VelopackVersion`

Estos valores alimentan:

- metadata de ensamblado
- titulo visible de la app
- identificador de aplicacion
- colores base
- pack script de Velopack
- release workflow

## Release architecture

Las releases son `tag-driven`.

Flujo:

1. Crear un tag semantico como `v0.3.1`
2. GitHub Actions instala `.NET 10`
3. Se instala el workload `maui`
4. `scripts/pack-app.ps1` publica y empaqueta la app para `win-x64`
5. Los artefactos se suben al GitHub Release

Ficheros principales:

- [release.yml](./.github/workflows/release.yml)
- [pack-app.ps1](./scripts/pack-app.ps1)
- [Directory.Build.props](./Directory.Build.props)

## Persistencia local

La plantilla usa persistencia JSON intencionalmente porque el escenario objetivo es:

- local-first
- sin backend obligatorio
- sin base de datos mientras no haga falta
- facil de entender y reemplazar en una app derivada

El ejemplo actual usa:

- `data/persistence.json`

## Como derivar una app concreta

Pasos minimos:

1. Cambiar los valores de `Directory.Build.props`.
2. Sustituir textos de `Home` por la descripcion real del producto.
3. Reemplazar `Persistence` por la primera feature real o adaptar su esquema JSON.
4. Mantener `Settings` como superficie de diagnostico y actualizacion.
5. Actualizar iconos, splash y assets.
6. Configurar `TemplateRepositoryUrl` con el repo real que publicara releases.
7. Mantener el workflow por tags `v*.*.*`.

## Estado

`Ofegabous` no pretende ser una app final. Es una plantilla evolucionable.

Lo que ya debe considerarse estable:

- stack de runtime
- enfoque Windows-only
- estructura de servicios
- persistencia local simple
- flujo de actualizacion
- release automation

Lo que se espera derivar o reemplazar:

- branding
- naming final del producto
- paginas de negocio
- esquema de datos
- UX especifica de cada app
