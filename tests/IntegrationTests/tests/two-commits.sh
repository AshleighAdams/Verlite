#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

setup_git
git commit --allow-empty -m "first" > /dev/null
git commit --allow-empty -m "second" > /dev/null

assert "0.1.0-alpha.2" dotnet verlite
assert "1.2.3-alpha.2" dotnet verlite --min-version 1.2.3
assert "1.2.3-beta.4.2" dotnet verlite --min-version 1.2.3-beta.4
assert "4.3.2" dotnet verlite --version-override=4.3.2
assert "alpha.2" dotnet verlite --show prerelease
assert "beta.4.2" dotnet verlite --min-version 1.2.3-beta.4 --show prerelease
assert "" dotnet verlite --version-override=1.0.0 --show prerelease

dotnet verlite --auto-fetch > /dev/null