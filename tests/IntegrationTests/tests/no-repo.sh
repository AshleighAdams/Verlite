#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null

if dotnet verlite $VERBOSE 2> /dev/null; then
	echo "Failed, verlite returned no error in absent repo."
	exit 1
fi