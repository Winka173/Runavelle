#!/usr/bin/env bash
# Downloads Unity reference assemblies (NuGet, dev-only, NOT committed) used to compile-check the Unity layer
# without a Unity Editor: UnityEngine.Modules 2021.3.33 + UnityEditor 2018.1.6 (editor API subset used is stable). API subset used is stable in Unity 6.
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)/.unityrefs"
mkdir -p "$DIR" && cd "$DIR"
fetch() { [ -d "$1" ] || { curl -sSL "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg" -o "$1.zip" && mkdir -p "$1" && unzip -q -o "$1.zip" -d "$1" && rm "$1.zip"; }; }
fetch unityengine.modules 2021.3.33
fetch unity3d.unityeditor 2018.1.6-f1
echo "refs in $DIR"
