#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null

git init > /dev/null

[[ $(dotnet verlite $VERBOSE) == "0.1.0-alpha.1" ]]
[[ $(dotnet verlite --min-version 1.2.3 $VERBOSE) == "1.2.3-alpha.1" ]]
[[ $(dotnet verlite --min-version 1.2.3-beta.4 $VERBOSE) == "1.2.3-beta.4.1" ]]

dotnet verlite $VERBOSE --auto-fetch > /dev/null