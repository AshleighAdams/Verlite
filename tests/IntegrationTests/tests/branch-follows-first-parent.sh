#!/bin/bash
set -euo pipefail

dotnet new tool-manifest > /dev/null
dotnet tool install Verlite.CLI --version 0.0.0 > /dev/null
dotnet tool restore > /dev/null

git init > /dev/null > /dev/null
git config commit.gpgsign false > /dev/null
git commit --allow-empty -m "first" > /dev/null
git commit --allow-empty -m "second" > /dev/null
git tag v1.0.0 > /dev/null

git checkout -b some-branch &> /dev/null

git commit --allow-empty -m "branch1" > /dev/null
git commit --allow-empty -m "branch2" > /dev/null
git commit --allow-empty -m "branch3" > /dev/null
git commit --allow-empty -m "branch4" > /dev/null
git commit --allow-empty -m "branch5" > /dev/null

git checkout master &> /dev/null
git merge some-branch --no-ff > /dev/null

assert "1.0.1-alpha.1" dotnet verlite

git checkout master^2 &> /dev/null

assert "1.0.1-alpha.5" dotnet verlite