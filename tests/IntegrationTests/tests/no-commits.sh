#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null

git init > /dev/null

[[ $(dotnet verlite $VERBOSE) == "v0.1.0-alpha.1" ]]