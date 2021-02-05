#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

git init > /dev/null > /dev/null
git config commit.gpgsign false > /dev/null
git commit --allow-empty -m "first" > /dev/null
git tag v2.3.4 > /dev/null

assert "2.3.4" dotnet verlite

git commit --allow-empty -m "second" > /dev/null

assert "2.3.5-alpha.1" dotnet verlite
assert "2.4.0-alpha.1" dotnet verlite --auto-increment minor
assert "3.0.0-alpha.1" dotnet verlite --auto-increment major
assert "2.3.5-alpha.0" dotnet verlite --prerelease-base-height 0
assert "2.3.5-beta.1" dotnet verlite --default-prerelease-phase beta

git tag v2.3.5-rc.1 > /dev/null

assert "2.3.5-rc.1" dotnet verlite

dotnet verlite --auto-fetch > /dev/null