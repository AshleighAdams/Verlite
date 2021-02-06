#!/bin/bash
set -euo pipefail

git init > /dev/null > /dev/null
git config commit.gpgsign false > /dev/null
git commit --allow-empty -m "first" > /dev/null
git tag v1.0.0 > /dev/null
git tag 2.0.0 > /dev/null
git tag abc/3.0.0 > /dev/null
git commit --allow-empty -m "second" > /dev/null

dotnet pack -o artifacts > /dev/null

should-exist() {
	if [[ ! -f "$1" ]]; then
		echo "Missing artifact: $1"
		exit 1
	fi
}

should-exist artifacts/Normal.1.0.1-alpha.1.nupkg
should-exist artifacts/AutoIncrement.1.1.0-alpha.1.nupkg
should-exist artifacts/DisableTagPrefix.2.0.1-alpha.1.nupkg
should-exist artifacts/TagPrefix.3.0.1-alpha.1.nupkg
should-exist artifacts/MinimumVersion.4.0.0-alpha.1.nupkg
should-exist artifacts/VersionOverride.1.33.7.nupkg
