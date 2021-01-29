# Verlite

[![Verlite.MsBuild][verlite-msbuild-badge]][verlite-msbuild-link]
[![Verlite.CLI][verlite-cli-badge]][verlite-cli-link]
[![Verlite.Core][verlite-core-badge]][verlite-core-link]

## Options

| Description                                                         | CLI Short, CLI Long, MsBuild Property                         | Default |
| :------------------------------------------------------------------ | :------------------------------------------------------------ | :------ |
| Disable invoking Verlite.                                           | VerliteDisabled                                               | false   |
| Tags starting with this represent versions.                         | -t, --tag-prefix, VerliteTagPrefix                            | v       |
| Disable the version prefix.                                         | VerliteDisableTagPrefix                                       | false   |
| The default phase for the prerlease label.                          | -d, --default-prerelease-phase, VerliteDefaultPrereleasePhase | alpha   |
| The minimum RTM version, i.e the destined version.                  | -m, --min-version, VerliteMinimumVersion                      | 0.1.0   |
| The height for continious deliverable auto heights should begin at. | -p, --prerelease-base-height, VerlitePrereleaseBaseHeight     | 1       |
| Force the calculated version to be this version.                    | --version-override, VerliteVersionOverride                    |         |
| Logging level.                                                      | --verbosity, VerliteVerbosity                                 | Normal  |
| Set the build data to this value.                                   | -b, --build-metadata, VerliteBuildMetadata                    |         |
| Part of the version to print.                                       | -s, --show                                                    | All     |
| Automatically fetch commits and a tag for shallow clones.           | -a, --auto-fetch                                              | false   |

To make Verlite behave the same as MinVer's default settings, set the following properties in your projects:

```xml
<PropertyGroup>
	<VerliteDisableTagPrefix>true</VerliteDisableTagPrefix>
	<VerliteDefaultPrereleasePhase>alpha.0</VerliteDefaultPrereleasePhase>
<PropertyGroup>
```

## Deepen

```sh
git ls-remote --tags "$(git ls-remote --get-url)" "refs/tags/v*"

# deepen until a hash is seen
# fetch the seen tag
```

[verlite-msbuild-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.MsBuild
[verlite-msbuild-link]: https://www.nuget.org/packages/Verlite.MsBuild
[verlite-cli-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.CLI
[verlite-cli-link]: https://www.nuget.org/packages/Verlite.CLI
[verlite-core-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.Core
[verlite-core-link]: https://www.nuget.org/packages/Verlite.Core
