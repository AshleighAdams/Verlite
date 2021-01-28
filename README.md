# Verlite

## Options

| Description                                                         | CLI Short, CLI Long, MsBuild Property                         | Default |
| ------------------------------------------------------------------- | :------------------------------------------------------------ | :------ |
| Disable invoking Verlite.                                           | VerliteDisabled                                               | false   |
| Tags starting with this represent versions.                         | -t, --tag-prefix, VerliteTagPrefix                            | v       |
| Disable the version prefix.                                         | VerliteDisableTagPrefix                                       | false   |
| The default phase for the prerlease label.                          | -d, --default-prerelease-phase, VerliteDefaultPrereleasePhase | alpha   |
| The minimum RTM version, i.e the destined version.                  | -m, --min-version, VerliteMinimumVersion                      | 0.1.0   |
| The height for continious deliverable auto heights should begin at. | -p, --prerelease-base-height, VerlitePrereleaseBaseHeight     | 1       |
| Force the calculated version to be this version.                    | --version-override, VerliteVersionOverride                    |         |
| Logging level.                                                      | --verbosity, VerliteVerbosity                                 | Normal  |
| Set the build data to this value.                                   | -b, --build-metadata, VerliteBuildMetadata                    |         |
| Part of the version to print.                                       | -b, --show,                                                   | All     |

To make Verlite behave the same as MinVer's default settings, set the following properties in your projects:

```xml
<PropertyGroup>
	<VerliteDisableTagPrefix>true</VerliteDisableTagPrefix>
	<VerliteDefaultPrereleasePhase>alpha.0</VerliteDefaultPrereleasePhase>
<PropertyGroup>
```
