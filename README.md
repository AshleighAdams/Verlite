# Verlite

[![Verlite.MsBuild][verlite-msbuild-badge]][verlite-msbuild-link] [![Verlite.CLI][verlite-cli-badge]][verlite-cli-link] [![Verlite.Core][verlite-core-badge]][verlite-core-link]

Versioning with [SemVer 2][semver-2] Git tags. Automatically set the version for SDK projects or use the CLI tool for all others. Platform agnostic.

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
| Automatically fetch commits and a tag for shallow clones.           | -a, --auto-fetch                                                 | false   |

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
