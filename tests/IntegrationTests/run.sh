#!/bin/bash
set -euo pipefail

cd "$(dirname $0)"

export VERBOSE=""
export REPO_PATH="$(git rev-parse --show-toplevel)"

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
	cp NuGet.conf.disabled "${tmp}/NuGet.Config"
	pushd "${tmp}" > /dev/null
		chmod +x "${script}"
		"${script}"
	popd > /dev/null
}

assert() {
	local expected="$1"
	shift
	local got="$($@)" 2> /dev/null
	if [[ "$expected" != "$got" ]]; then
		echo "Assertion failed: $@"
		echo "Expected $expected but got $got"
		exit 1
	fi
}
export -f assert

setup_git() {
	git init --initial-branch=master > /dev/null
	git config user.email "integratio@test.tld"
	git config user.name "Integration Test"
	git config commit.gpgsign false > /dev/null
	git config tag.gpgSign false > /dev/null
}
export -f setup_git

echo "Running tests"

test arg-escaping
test no-repo
test no-commits
test one-commit
test one-commit-tagged
test two-commits
test two-commits-tagged
test branch-follows-first-parent
test msbuild
test multiple-tags-on-same-commit
test auto-fetch
test publish-different-framework

echo "All complete."