#!/bin/bash
set -euo pipefail

git clone "$REPO_PATH" verlite > /dev/null 2> /dev/null

dotnet publish \
    verlite/src/Verlite.CLI/Verlite.CLI.csproj \
    -o artifacts \
    -p:TargetFramework=net5.0 \
    -p:PackAsTool=false \
> /dev/null 2> /dev/null

dotnet publish \
    verlite/src/Verlite.CLI/Verlite.CLI.csproj \
    -o artifacts2 \
    -f=net5.0 \
    -p:PackAsTool=false \
> /dev/null 2> /dev/null

should-exist() {
	if [[ ! -f "$1" ]]; then
		echo "Missing artifact: $1"
		exit 1
	fi
}

should-exist artifacts/Verlite.CLI.dll
should-exist artifacts2/Verlite.CLI.dll