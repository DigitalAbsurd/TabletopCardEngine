#!/usr/bin/env bash
# Checks a `dotnet pack TabletopCardEngine.Packages.slnf` output directory: one .nupkg per project of
# the solution filter, and every one of them carrying the expected version. MinVer takes the version
# from the tag at HEAD, so a shallow checkout or a checkout of a branch fails here rather than on
# nuget.org. Used by release.yml after its pack and by publish-nuget.yml before it pushes.
#
# Usage: eng/verify-packages.sh <dir> <version>    (the version without the `v`, e.g. 1.0.0-preview.1)
set -euo pipefail

if [ "$#" -ne 2 ]; then
  echo "::error::Usage: $0 <dir> <version>" >&2
  exit 2
fi

dir="$1"
version="$2"
root="$(cd "$(dirname "$0")/.." && pwd)"

if [ ! -d "$dir" ]; then
  echo "::error::$dir is not a directory"
  exit 1
fi

# One project per line in the solution filter; grep keeps the script free of a jq dependency.
expected=$(grep -c '\.csproj"' "$root/TabletopCardEngine.Packages.slnf")

shopt -s nullglob
packages=("$dir"/*.nupkg)
shopt -u nullglob

bad=0
if [ "${#packages[@]}" -ne "$expected" ]; then
  bad=1
fi
for package in "${packages[@]}"; do
  case "$package" in
    *".$version.nupkg") ;;
    *) bad=1 ;;
  esac
done

if [ "$bad" -ne 0 ]; then
  echo "::error::Expected $expected packages ending in .$version.nupkg in $dir; found ${#packages[@]}:"
  ls "$dir"
  exit 1
fi
echo "${#packages[@]} packages carry version $version"
