#!/bin/bash
set -euo pipefail

export VERBOSE=""

# setup the packages as version 0.0.0

echo "Building NuGet packages as v0.0.0..."
[[ -d packages ]] && rm -rf packages
MinVerVersionOverride=0.0.0 dotnet pack ../../Verlite.sln -o packages

test() {
	echo "  $1..."
	local script="$(pwd)/tests/$1.sh"
	local tmp="$(mktemp -d)"
	cp -r "packages" "${tmp}/packages"
	cp NuGet.conf.disabled "${tmp}/NuGet.conf"
	pushd "${tmp}" > /dev/null
		"${script}"
	popd > /dev/null
}

echo "Beggining tests"

test no-repo
test no-commits

echo "All complete."