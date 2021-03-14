#!/bin/bash
set -euo pipefail

arch="$(uname -m)"
if [[ "$arch" == "x86_64" ]]; then
	runtime="x64"
elif [[ "$arch" == "amd64" ]]; then
	runtime="x64"
else
	echo "Arch $arch not mapped" > /dev/stderr
	exit 1
fi

uos="$(uname -s)"
if [[ "$uos" == "MINGW"* ]]; then
	runtime="win-$runtime"
	exe="TestEscaping.exe"
elif [[ "$uos" == "Linux"* ]]; then
	runtime="linux-$runtime"
	exe="TestEscaping"
elif [[ "$uos" == "Darwin"* ]]; then
	runtime="osx-$runtime"
	exe="TestEscaping"
else
	echo "OS $uos not mapped" > /dev/stderr
	exit 1
fi

echo '<?xml version="1.0" encoding="utf-8"?>' > NuGet.Config
echo '<configuration>' >> NuGet.Config
echo '  <packageSources>' >> NuGet.Config
echo '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' >> NuGet.Config
echo '    <add key="local" value="packages" />' >> NuGet.Config
echo '  </packageSources>' >> NuGet.Config
echo '</configuration>' >> NuGet.Config

dotnet publish src/TestEscaping/TestEscaping.csproj --output out --runtime "$runtime" > /dev/null

if [[ ! -f "./out/$exe" ]]; then
	echo "Executable not found, out contents:"
	ls out
	exit 1
fi

"./out/$exe"