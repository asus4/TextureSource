#!/bin/bash
#
# Create a GitHub release draft from a tgz package file
# Usage: ./scripts/create-release.sh <tgz-file-path>
#

set -euo pipefail

# Validate arguments
if [ $# -ne 1 ]; then
    echo "Usage: $0 <tgz-file-path>" >&2
    exit 1
fi

TGZ_FILE="$1"

# Verify file exists
[ -f "$TGZ_FILE" ] || {
    echo "Error: File not found: $TGZ_FILE" >&2
    exit 1
}

# Extract version from package.json inside the tgz
VERSION=$(tar -xzOf "$TGZ_FILE" package/package.json | \
    node -p "JSON.parse(require('fs').readFileSync(0, 'utf-8')).version || ''")

[ -n "$VERSION" ] || {
    echo "Error: Version not found in package.json" >&2
    exit 1
}

TAG="v${VERSION}"
echo "Creating release: $TAG"

# Create GitHub release draft with the tgz file attached
gh release create "$TAG" \
    --draft \
    --title "$TAG" \
    --generate-notes \
    "$TGZ_FILE"

echo "✅ Release draft created successfully!"

# Publish to NPM
npm publish "$TGZ_FILE" --tag "$TAG"
echo "✅ Published to NPM successfully!"
