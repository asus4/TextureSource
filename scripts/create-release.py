#!/usr/bin/env python3
"""
Create a GitHub release draft from a tgz package file.

Usage: python scripts/create-release.py <tgz-file-path>
"""

import argparse
import json
import subprocess
import sys
import tarfile
from pathlib import Path


def get_version_from_tgz(tgz_file: Path) -> str:
    """Extract version from package.json inside the tgz file"""
    with tarfile.open(tgz_file, 'r:gz') as tar:
        try:
            member = tar.getmember('package/package.json')
        except KeyError:
            raise ValueError(f"package.json not found in {tgz_file}") from None

        if not (file_obj := tar.extractfile(member)):
            raise ValueError("Failed to extract package.json")

        data = json.load(file_obj)
        if not (version := data.get('version')):
            raise ValueError("Version not found in package.json")

        return version

def get_repo_name() -> str:
    """Get repository name with owner (e.g., 'owner/repo')"""
    result = subprocess.run(
        ['gh', 'repo', 'view', '--json', 'nameWithOwner', '-q', '.nameWithOwner'],
        capture_output=True,
        text=True,
        check=True
    )
    return result.stdout.strip()


def create_release(tag: str, tgz_file: Path) -> None:
    """Create GitHub release draft and upload tgz file"""
    subprocess.run(
        [
            'gh', 'release', 'create', tag,
            '--draft',
            '--title', tag,
            '--generate-notes',
            str(tgz_file)
        ],
        check=True
    )


def main():
    parser = argparse.ArgumentParser(
        description='Create a GitHub release draft and upload tgz file'
    )
    parser.add_argument('tgz_file', type=Path, help='Path to the .tgz file')
    args = parser.parse_args()

    try:
        if not args.tgz_file.exists():
            raise FileNotFoundError(f"File does not exist: {args.tgz_file}")

        version = get_version_from_tgz(args.tgz_file)
        tag = f"v{version}"
        print(f"Version: {version}, Tag: {tag}")

        create_release(tag, args.tgz_file)
        repo_name = get_repo_name()

        print(f"""✅ Release draft created successfully!
Open: https://github.com/{repo_name}/releases/tag/{tag}""")
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == '__main__':
    main()
