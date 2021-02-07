<img src=".meta/Verlite.svg" align="right" width="20%" alt="Logo" />

# Verlite

[![Verlite.MsBuild][verlite-msbuild-badge]][verlite-msbuild-link] [![Verlite.CLI][verlite-cli-badge]][verlite-cli-link] [![Verlite.Core][verlite-core-badge]][verlite-core-link] [![Codecov][codecov-badge]][codecov-link]

Versioning with [SemVer 2][semver-2] Git tags. Automatically set the version for SDK-style projects or use the CLI tool for all others. Platform agnostic.

## Usage

Add the following to your `Directory.Build.props` or csproj:

```xml
<ItemGroup>
  <PackageReference Include="Verlite.MsBuild" Version="x.y.z" PrivateAssets="All" />
</ItemGroup>
```

Optionally if your CI/CD pipelines use shallow clones, add this to your build steps to deepen the repository to&mdash;and fetch&mdash;the nearest tag:

```sh
dotnet tool install --global Verlite.CLI --version "x.y.z"
verlite --auto-fetch
```

## Goals and Scope

Verlite falls falls between MinVer and GitVersion&mdash;using the same versioning scheme as MinVer with more flexibility, and providing some of the more crucial features of GitVersion.

Verlite is aimed at  continuous delivery workflows, not continuous deployment workflows&mdash;where versions are denoted from branching model or commit messages. Instead with Verlite, tags are the source of truth for versions. Any versions with height attached (see [version calculation](#version-calculation)) are intended only for development purposes and to not be released to the primary feed.

Versioning based upon commit messages or branches is out of scope. Such can be done via passing different [options](#options) into Verlite by your build process, but keep in mind this is not a supported workflow of Verlite, so shouldn't be done for release critical aspects.

## Version Calculation

Take the head commit, if one or more version tags exist, use the highest version, otherwise, keep following the first parent of each commit until a version tag is found, taking the highest version tag, then bumping the version and appending the "commit height" onto the end.

To bump the version, the patch is by default incremented by 1. The version part to bump can be configured via `<VerliteAutoIncrement>`/`--auto-increment` option.

The commit height is applied by concatenating the prerelease tag, a separator ("."), and the height together, where the prerelease tag is either the last tagged version's prerelease, or if not found/was not a prerelease, using the `<VerliteDefaultPrereleasePhase>`/`--default-prerelease-phase `option.

## Options

| Description                                                         | CLI Short, CLI Long, MsBuild Property                            | Default |
| :------------------------------------------------------------------ | :--------------------------------------------------------------- | :------ |
| Disable invoking Verlite.                                           | VerliteDisabled, --tag-prefix ""                                 | false   |
| Tags starting with this represent versions.                         | -t, --tag-prefix, VerliteTagPrefix                               | v       |
| Disable the version prefix.                                         | VerliteDisableTagPrefix                                          | false   |
| The default phase for the prerelease label                          | -d, --default-prerelease-phase, VerliteDefaultPrereleasePhase    | alpha   |
| The minimum RTM version, i.e the destined version.                  | -m, --min-version, VerliteMinimumVersion                         | 0.1.0   |
| The height for continuous deliverable auto heights should begin at. | -p, --prerelease-base-height, VerlitePrereleaseBaseHeight        | 1       |
| Force the calculated version to be this version.                    | --version-override, VerliteVersionOverride                       |         |
| Logging level.                                                      | --verbosity, VerliteVerbosity                                    | Normal  |
| Set the build data to this value.                                   | -b, --build-metadata, VerliteBuildMetadata                       |         |
| Part of the version to print.                                       | -s, --show                                                       | All     |
| Automatically fetch commits and a tag for shallow clones.           | --auto-fetch                                                     | false   |
| Set which version part should be bumped after an RTM release.       | -a, --auto-increment                                             | patch   |

# Comparison with GitVersion

GitVersion has a focus on branches, and is well suited for a Continuous Deploy workflow, where releases happen based upon branches. Shallow repositories are not supported.

Verlite only cares about tags, particularly the tags on the chain of first parents, and is well suited for Continuous Delivery, where official releases happen by pushing tags.

# Comparison with MinVer

MinVer's default behavior can be considered a subset of Verlite, and so we can configured Verlite to behave the same with the following properties set:

```xml
<PropertyGroup>
	<VerliteDisableTagPrefix>true</VerliteDisableTagPrefix>
	<VerliteDefaultPrereleasePhase>alpha.0</VerliteDefaultPrereleasePhase>
<PropertyGroup>
```

Verlite has some additional features, some of which I required, hence the creation of this project. These are:

 - Shallow repositories are fully supported.
	- Fetch tags and commits needed for calculating the version with `verlite --auto-fetch`.
	- Error out if the repository is too shallow instead of silently returning an invalid version.
 - Continuous Delivery versions can start at the first prelease ordinal to reduce long version fatigue.
   That is to say, after tagging `1.0.0`, the next CD version by default is `1.0.1-alpha.1` instead of `1.0.1-alpha.0.1`.
	- CD releases after a tagged prerelease behave identical to MinVer, for example, the commit after `1.0.0-rc.1` becomes `1.0.0-rc.1.1` and not `1.0.0-rc.2`.
 - The default base height after a tag can be set, such as `1.0.0` -> `1.0.1-alpha.0`.
 - Scripts can query Verlite for a specific version part.


[verlite-msbuild-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.MsBuild
[verlite-msbuild-link]: https://www.nuget.org/packages/Verlite.MsBuild
[verlite-cli-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.CLI
[verlite-cli-link]: https://www.nuget.org/packages/Verlite.CLI
[verlite-core-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.Core
[verlite-core-link]: https://www.nuget.org/packages/Verlite.Core
[semver-2]: https://semver.org/spec/v2.0.0.html
[codecov-badge]: https://codecov.io/gh/AshleighAdams/Verlite/branch/master/graph/badge.svg?token=ZE1ITHB3U3
[codecov-link]: https://codecov.io/gh/AshleighAdams/Verlite
