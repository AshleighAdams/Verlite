#!/bin/bash
set -euo pipefail

cd "$(dirname $0)"

export VERBOSE=""

# setup the packages as version 0.0.0
export NUGET_PACKAGES="$(mktemp -d)"
echo "Building NuGet packages as v0.0.0... (cache $NUGET_PACKAGES)"
dotnet nuget locals global-packages -c
[[ -d packages ]] && rm -rf packages
MinVerVersionOverride=0.0.0 dotnet pack ../../Verlite.sln -o packages

test() {
	echo "  $1..."
	local script="$(pwd)/tests/$1.sh"
	local tmp="$(mktemp -d)"
	[[ -d "tests/$1" ]] && cp -r "tests/$1/"* "${tmp}/"
	cp -r "packages" "${tmp}/packages"
	cp NuGet.conf.disabled "${tmp}/NuGet.config"
	pushd "${tmp}" > /dev/null
		"${script}"
	popd > /dev/null
}

assert() {
	local expected="$1"
	shift
	local got="$($@)"
	if [[ "$expected" != "$got" ]]; then
		echo "Assertion failed: $@"
		echo "Expected $expected but got $got"
		exit 1
	fi
}
export -f assert

echo "Running tests"

test no-repo
test no-commits
test one-commit
test one-commit-tagged
test two-commits
test two-commits-tagged
test branch-follows-first-parent
test msbuild

echo "All complete."