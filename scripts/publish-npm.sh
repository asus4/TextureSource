#!/bin/bash
#
# Publish the signed tgz from a GitHub release to npm.
# Run locally after the release is published (requires `npm login` and `gh auth login`).
#
# Usage: ./scripts/publish-npm.sh [tag]
#   tag: release tag like v0.4.0 (defaults to the latest release)
#

set -euo pipefail

TAG="${1:-}"
if [ -z "$TAG" ]; then
    TAG=$(gh release view --json tagName --jq '.tagName')
    echo "No tag specified, using latest release: $TAG"
fi

# Download the signed tgz from the release
DOWNLOAD_DIR=$(mktemp -d)
trap 'rm -rf "$DOWNLOAD_DIR"' EXIT
gh release download "$TAG" --pattern '*.tgz' --dir "$DOWNLOAD_DIR"

TGZ_FILE=$(find "$DOWNLOAD_DIR" -name '*.tgz' | head -n 1)
[ -n "$TGZ_FILE" ] || {
    echo "Error: No tgz asset found in release $TAG" >&2
    exit 1
}

# Verify the tag matches the package version inside the tgz
VERSION=$(tar -xzOf "$TGZ_FILE" package/package.json | \
    node -p "JSON.parse(require('fs').readFileSync(0, 'utf-8')).version || ''")
[ "v$VERSION" = "$TAG" ] || {
    echo "Error: Tag $TAG does not match package version $VERSION" >&2
    exit 1
}

echo "Publishing $(basename "$TGZ_FILE") to npm"

# No --provenance: npm provenance is incompatible with signed tgz archives
npm publish "$TGZ_FILE" --access public --tag latest

echo "✅ Published to npm successfully!"
