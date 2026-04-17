# Runtime Model

The template distinguishes between bundled content and persistent user data.

## Two distinct roots

### Content root

This is where the app looks for content distributed with the build:

- the `documentation` folder
- example data included with the app

In development it matches the repo root.

In a Velopack installation it matches the directory where the current version of the app lives.

### Storage root

This is where the app stores data that must survive updates:

- the `data` folder
- the `persistence.json` file

In development it matches the repo root.

In a Velopack installation it resolves outside `current`, so user data is not lost when the app updates.

## Startup model

1. The current environment is resolved.
2. The Velopack startup state is recorded.
3. The app loads the MAUI Blazor Hybrid shell.
4. `Settings` and the side menu read the update state.
5. `Persistence` accesses the local JSON file.
6. `Documentation` reads markdown from the `documentation` folder.

## Development mode

The template treats `dev mode` intentionally:

- it shows version `x.x.x-dev`
- it does not run real updates
- it still explains why updates are disabled

## Why this separation helps

Separating bundled content from persistent data allows you to:

- update the app without losing user data
- keep documentation bundled with the distribution
- preserve a consistent experience between development and installed runtime
