[![Codecov][codecov-badge]][codecov-link] [![Mutation testing score][mutation-testing-badge]][mutation-testing-link]

Automatically compute versions from a Git repo via [SemVer 2][semver-2] Git tags, and print the computed version to stdout.

For Continuous Delivery workflows.

## Quickstart

Install as a global tool:

```sh
dotnet tool install --global Verlite.CLI
verlite --help
```

## Full readme

[View the full readme on GitHub][full-readme] for available options, an FAQ, and in depth details.


[full-readme]: https://github.com/AshleighAdams/Verlite/blob/master/README.md
[semver-2]: https://semver.org/spec/v2.0.0.html
[codecov-badge]: https://codecov.io/gh/AshleighAdams/Verlite/branch/master/graph/badge.svg?token=ZE1ITHB3U3
[codecov-link]: https://codecov.io/gh/AshleighAdams/Verlite
[mutation-testing-badge]: https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2FAshleighAdams%2FVerlite%2Fmaster
[mutation-testing-link]: https://dashboard.stryker-mutator.io/reports/github.com/AshleighAdams/Verlite/master
