#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

setup_git
git commit --allow-empty -m "first" > /dev/null

git tag v1.0.0-alpha.1 > /dev/null
assert "1.0.0-alpha.1" dotnet verlite
git tag v1.0.0-rc.1 > /dev/null
assert "1.0.0-rc.1" dotnet verlite
git tag v1.0.0 > /dev/null
assert "1.0.0" dotnet verlite
git tag v1.0.0-rc.2 > /dev/null
assert "1.0.0" dotnet verlite

git commit --allow-empty -m "second" > /dev/null

assert "1.0.1-alpha.1" dotnet verlite