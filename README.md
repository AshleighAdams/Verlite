<img src=".meta/Verlite.svg" align="right" width="20%" alt="Logo" />

# Verlite

[![Verlite.MsBuild][verlite-msbuild-badge]][verlite-msbuild-link] [![Verlite.CLI][verlite-cli-badge]][verlite-cli-link] [![Verlite.Core][verlite-core-badge]][verlite-core-link] [![Codecov][codecov-badge]][codecov-link]

Versioning with [SemVer 2][semver-2] Git tags. Automatically set the version from Git tags for .NET Core SDK-style projects, or use the CLI tool for all others. Platform agnostic.

## Usage

Add the following to your `Directory.Build.props` or `csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Verlite.MsBuild" Version="x.y.z" PrivateAssets="All" />
</ItemGroup>
```

Optionally, if your CI/CD pipelines use shallow clones (such as GitHub Actions by default), add build steps to automatically deepen the repository to&mdash;and fetch&mdash;the nearest tag:

```sh
dotnet tool install --global Verlite.CLI --version "x.y.z"
verlite --auto-fetch
```

## Goals and Scope

Verlite aims to fall somewhere between MinVer and GitVersion&mdash;using the same versioning scheme as MinVer, with a slightly richer and more flexible feature set.

Verlite is aimed at  continuous delivery workflows, not continuous deployment workflows&mdash;where versions are denoted from a branching model or commit messages. Instead with Verlite, tags are the source of truth for versions. Any versions with height attached (see [version calculation](#version-calculation)) are intended only for development purposes and to not be released to the primary feed.

Versioning based upon commit messages or branches is out of scope. Such can be done via passing different [options](#options) into Verlite by your build process, but keep in mind this is not a supported workflow of Verlite, so shouldn't be done for release critical aspects.

## Version Calculation

Take the head commit, if one or more version tags exist, use the highest version, otherwise, keep following the first parent of each commit until a version tag is found, taking the highest version tag, then bumping the version and appending the "commit height" onto the end.

To bump the version, the patch is by default incremented by 1. The version part to bump can be configured via `VerliteAutoIncrement`/`--auto-increment` option.

The commit height is applied by concatenating the prerelease tag, a separator ("."), and the height together, where the prerelease tag is either the last tagged version's prerelease, or if not found/was not a prerelease, using the `VerliteDefaultPrereleasePhase`/`--default-prerelease-phase `option.

See [docs/VersionCalculation.md](docs/VersionCalculation.md) for further reading.

## Options

| Description                                                         | CLI Short, CLI Long, MsBuild Property                            | Default |
| :------------------------------------------------------------------ | :--------------------------------------------------------------- | :------ |
| Disable invoking Verlite.                                           | VerliteDisabled                                                  | false   |
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
| Set which version part should be bumped after an RTM release.       | -a, --auto-increment, VerliteAutoIncrement                       | patch   |

# Comparison with GitVersion

GitVersion has a focus on branches, and is well suited for a Continuous Deployment workflow, where releases are triggered based upon branches or commit messages. Shallow repositories are not supported.

Verlite cares only about tags—more specifically, the tags on the chain of first parents—and is well suited for Continuous Delivery workflows, where official releases happen by tagging.

# Comparison with MinVer

MinVer's behavior is a subset of Verlite, and so we can configured Verlite to behave the same with the following properties set:

```xml
<PropertyGroup>
	<VerliteDisableTagPrefix>true</VerliteDisableTagPrefix>
	<VerliteDefaultPrereleasePhase>alpha.0</VerliteDefaultPrereleasePhase>
<PropertyGroup>
```

Additionally, Verlite has some extra features, some of which I required or desired, hence the creation of this project. These are:

 - Shallow repositories are fully supported.
	- Fetch tags and commits needed for calculating the version with `verlite --auto-fetch`.
	- Error out if the repository is too shallow instead of silently returning an incorrect version.
 - Continuous Delivery versions can start at the first prelease ordinal to reduce long version fatigue.
   That is to say, after tagging `1.0.0`, the next CD version by default is `1.0.1-alpha.1` instead of `1.0.1-alpha.0.1`.
	- CD releases after a tagged prerelease behave identical to MinVer, for example, the commit after `1.0.0-rc.1` becomes `1.0.0-rc.1.1` and not `1.0.0-rc.2`.
 - The default base height after a tag can be set, such as `1.0.0` -> `1.0.1-alpha.0`.
 - Scripts can query Verlite for a specific version part.

## FAQ

 - [Why Verlite?](#why-verlite) *(simple and feature complete)*
 - [Can I bump the major or minor parts after an RTM tag?](#can-i-bump-the-major-or-minor-parts-after-an-rtm-tag) *(yes)*
 - [Can I change the default phase?](#can-i-change-the-default-phase) *(yes)*
 - [Why is the default phase "alpha" and not "alpha.0"?](#why-is-the-default-phase-alpha-and-not-alpha0) *(reduce fatigue)*
 - [Can prereleases be tagged?](#can-prereleases-be-tagged) *(yes, must be)*
 - [Can I use a branching strategy?](#can-i-use-a-branching-strategy) *(sort of)*
 - [Can Verlite be used elsewhere?](#can-verlite-be-used-elsewhere) *(yes)*
 - [What is the default tag prefix?](#what-is-the-default-tag-prefix) *("v")*
 - [Can multiple versions be nested?](#can-multiple-versions-be-nested) *(yes)*
 - [Can shallow clones be used?](#can-shallow-clones-be-used) *(yes)*
 - [What happens if auto fetch isn't used?](#what-happens-if-auto-fetch-isnt-used) *(nothing unsafe)*

### Why Verlite?

For if you find GitVersion too complex and MinVer too minimal for your needs. Verlite is a superset of MinVer, but takes on a small amount of complexity to provide a simpler to use tool.

### Can I bump the major or minor parts after an RTM tag?

Yes, the `VerliteAutoIncrement` option will specify which version part should be bumped after an RTM tag.

### Can I change the default phase?

Yes, the the default phase of `alpha` can be changed using the `VerliteDefaultPrereleasePhase` option.

### Why is the default phase "alpha" and not "alpha.0"?

In short, to reduce fatigue. The first commits after an RTM tag are more likely to be hotfixes bumping the patch instead of something to undergo various prerelease phases, and so to make Continuous Delivery builds less fatiguing to use, the default phase omits a number, seeing such builds be versioned as `1.0.1-alpha.42` instead of `1.0.1-alpha.0.42`. Then upon early prereleases, it is recommended to tag with a `beta` `prerelease` phase, such as `1.0.1-beta.1`, in which the next CD deliverables are versioned like `1.0.1-beta.1.42`.

Should the you prefer `alpha.0` be used instead, such can be done by changing the default phase.

### Can prereleases be tagged?

You should only release tagged prereleases. Then for subsequent untagged commits, they will be versioned with the tagged version with the height appended. For example, the next commit after `2.0.0-rc.1` may be versioned as `2.0.0-rc.1.1`.

### Can I use a branching strategy?

Sort of. Verlite is intended for tags to be the cause of a release, not an effect of a release.
Verlite is not aware of named branches, and will not natively take them into account for version calculation, instead using only the commit graph and tags for version calculation.

Should you chose to, Verlite can be configured to produce different versions using MsBuild's `Condition` attribute under CI pipelines, for example:

```xml
<!-- apply the PR number for PR builds -->
<PropertyGroup Condition="$(GITHUB_ACTIONS.StartsWith('refs/pull/'))">
  <VerliteBuildMetadata>pr.$(GITHUB_REF.Substring(10))</VerliteBuildMetadata>
</PropertyGroup>
<!-- apply the branch name for branch builds -->
<PropertyGroup Condition="$(GITHUB_ACTIONS.StartsWith('refs/heads/'))">
  <VerliteBuildMetadata>branch.$(GITHUB_REF.Substring(11))</VerliteBuildMetadata>
</PropertyGroup>
<!-- mark locally build builds with +local -->
<PropertyGroup Condition="'$(GITHUB_ACTIONS)' == ''">
  <VerliteBuildMetadata>local</VerliteBuildMetadata>
</PropertyGroup>
```

### Can Verlite be used elsewhere?

Yes, the command line tool can be used elsewhere, for example, in Conan packages:

```python
from six import StringIO

def Project(ConanFile):
    # ...
    def set_version(self):
        buf = StringIO()
        self.run(f"verlite --auto-fetch {self.recipe_folder}", output=buf)
        self.version = buf.getvalue()
```

### What is the default tag prefix?

The default tag prefix is `v`, so a tag of `v1.2.3` is interpreted as SemVer `1.2.3`.

The default prefix can be set to nothing by setting `VerliteDisableTagPrefix` to `true` for MsBuild, or `--tag-prefix=""` for the CLI. It can be changed to an arbitrary value setting `VerliteTagPrefix` or `--tag-prefix`.

### Can multiple versions be nested?

Yes, by setting a unique `VerliteTagPrefix` for each project.

### Can shallow clones be used?

Yes, with a caveat—for performance reasons `verlite --auto-fetch` must be invoked to deepen the repository prior to building. To avoid footguns, auto-fetching is not exposed under MsBuild due to needing to querying the remote for each project in the solution with auto-fetching enabled.

### What happens if auto fetch isn't used?

Nothing unsafe. In the event a clone is not deep enough, an exception will be thrown and the build will fail, instead of calculating an incorrect version number silently.

Footguns not included.


[verlite-msbuild-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.MsBuild
[verlite-msbuild-link]: https://www.nuget.org/packages/Verlite.MsBuild
[verlite-cli-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.CLI
[verlite-cli-link]: https://www.nuget.org/packages/Verlite.CLI
[verlite-core-badge]: https://img.shields.io/nuget/vpre/Verlite.MsBuild?label=Verlite.Core
[verlite-core-link]: https://www.nuget.org/packages/Verlite.Core
[semver-2]: https://semver.org/spec/v2.0.0.html
[codecov-badge]: https://codecov.io/gh/AshleighAdams/Verlite/branch/master/graph/badge.svg?token=ZE1ITHB3U3
[codecov-link]: https://codecov.io/gh/AshleighAdams/Verlite
