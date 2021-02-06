#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

setup_git
git commit --allow-empty -m "first" > /dev/null
git tag v1.2.3 > /dev/null
git tag test/2.3.4 > /dev/null

assert "1.2.3" dotnet verlite
assert "2.3.4" dotnet verlite --tag-prefix "test/"
assert "1.2.3" dotnet verlite --min-version 1.2.3
assert "2.3.4" dotnet verlite --min-version 1.2.3 --tag-prefix "test/"
assert "1.2.3" dotnet verlite --min-version 1.2.3-beta.4
assert "4.3.2" dotnet verlite --version-override=4.3.2
assert "4.3.2" dotnet verlite --version-override=4.3.2 --tag-prefix "test/"

dotnet verlite --auto-fetch > /dev/null