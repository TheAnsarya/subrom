#!/bin/bash
# Build RPM package for Subrom

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
VERSION="${VERSION:-1.2.0}"

echo "Building Subrom RPM package v$VERSION"

# Create rpmbuild directory structure
RPMBUILD_DIR="$SCRIPT_DIR/rpmbuild"
rm -rf "$RPMBUILD_DIR"
mkdir -p "$RPMBUILD_DIR"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

# Create source tarball
TARBALL_DIR="$RPMBUILD_DIR/SOURCES/subrom-$VERSION"
mkdir -p "$TARBALL_DIR"
cp -r "$ROOT_DIR"/* "$TARBALL_DIR/"
cd "$RPMBUILD_DIR/SOURCES"
tar -czf "subrom-$VERSION.tar.gz" "subrom-$VERSION"
rm -rf "$TARBALL_DIR"

# Copy spec file
cp "$SCRIPT_DIR/rpm/subrom.spec" "$RPMBUILD_DIR/SPECS/"

# Update version in spec file
sed -i "s/^Version:.*/Version:        $VERSION/" "$RPMBUILD_DIR/SPECS/subrom.spec"

# Build RPM
echo "Building RPM..."
rpmbuild --define "_topdir $RPMBUILD_DIR" -ba "$RPMBUILD_DIR/SPECS/subrom.spec"

# Copy output
mkdir -p "$SCRIPT_DIR/dist"
cp "$RPMBUILD_DIR/RPMS/x86_64/"*.rpm "$SCRIPT_DIR/dist/"

echo "Successfully built RPM package in $SCRIPT_DIR/dist/"
