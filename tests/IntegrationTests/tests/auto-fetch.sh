#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

clone_url="file://$(pwd)/upstream"

mkdir upstream > /dev/null
pushd upstream > /dev/null
	setup_git
	git commit --allow-empty -m "first" > /dev/null
	git tag v1.0.0 > /dev/null
	git commit --allow-empty -m "second" > /dev/null
	git commit --allow-empty -m "third" > /dev/null
popd > /dev/null

check_fail() {
	if "$@" &> /dev/null; then
		echo "exit code should be non-zero ($?): $@"
		exit 1
	fi
}

git clone $clone_url downstream --depth 1 &> /dev/null
pushd downstream > /dev/null
	check_fail dotnet verlite
	assert "1.0.1-alpha.2" dotnet verlite --auto-fetch
	assert "1.0.1-alpha.2" dotnet verlite
popd > /dev/null
