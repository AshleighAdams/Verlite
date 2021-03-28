# Filtering Tags

Instead of Verlite taking on task of somewhat common reasons to disregard tags, such as checking for signed commits, ignoring automatic CD tags, Verlite instead inverts and deferrers this ability to your project by the use of a supplied command line's return code being used to determine if Verlite should use a tag in version calculation.

You can specify the command line for Verlite to use by setting `--filter-tags` for Verlite.CLI, or setting the `VerliteFilterTags`  property for Verlite.MsBuild. In the command line, any sequences of `{}` will be replaced with the tag under question before being executed. To add a `{` or `}` to your command, then it must be escaped by doubling the character.

The following environment variables are also set to aid in scripts with more advanced behaviors.

| Variable                   | Example          | Description                                                 |
| -------------------------- | ---------------- | ----------------------------------------------------------- |
| VERLITE_PATH               | .                | The path of the repository passed into Verlite.             |
| VERLITE_COMMIT             | 9671a81…         | The commit that the tag under question points to.           |
| VERLITE_TAG                | v1.2.3-rc.4      | The tag under question. Identical to the `{}` substitution. |
| VERLITE_VERSION            | 1.2.3-rc.4+git.5 | The interpreted version from the tag, in full.              |
| VERLITE_VERSION_MAJOR      | 1                | The interpreted major version.                              |
| VERLITE_VERSION_MINOR      | 2                | The interpreted minor version.                              |
| VERLITE_VERSION_PATCH      | 3                | The interpreted patch version.                              |
| VERLITE_VERSION_PRERELEASE | rc.4             | The interpreted prerelease. Empty string if not present.    |
| VERLITE_VERSION_BUILDMETA  | git.5            | The interpreted build meta. Empty string if not present.    |

## Examples

### Signed and verified tags

This example is simple, the functionality is built right into Git, so we can invoke it directly with no changes between different platforms.

```xml
<!-- Directory.Build.props or .csproj -->
<PropertyGroup>
	<VerliteFilterTags>git tag --verify {}</VerliteFilterTags>
</PropertyGroup>
```

### Ignore automatic CD tags

Advanced scripts can be invoked. It is safe to assume `bash` is in the path with Git installs, so we can invoke a shell script with these more advanced behaviors. For this example, if we assume build server tags contain "#auto-tag" in the annotation message, we can then filter them out for the purposes of version calculation.

As we're using a script on disk, we should also fully encode the path to it. That includes escaping the path too. This example will use `tag-filter.sh` next to the `Directory.Build.props`. On Unix OSes, `$(VerliteBashPath)` will expand to `bash`, and on Windows, it will attempt to locate and escape the path to Git's Bash.

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <VerliteFilterTags>
    $(VerliteBashPath) "$(MSBuildThisFileDirectory.Replace('\', '\\').Replace('"', '\\"'))tag-filter.sh"
  </VerliteFilterTags>
</PropertyGroup>
```

```bash
# tag-filter.sh
#!/bin/bash
body="$(git tag -n999 --format='%(contents:body)' "$VERLITE_TAG")"
if [[ "$body" == *"#auto-tag"* ]]; then
	echo tag contains keyword
	exit 1
else
	echo tag does not contain keyword
	exit 0
fi
```
