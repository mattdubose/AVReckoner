#!/usr/bin/env bash
# Package AvReckoner as a distributable macOS .app + .dmg (arm64, unsigned).
#
# Usage: ./package_mac.sh
#
# Produces:
#   dist_mac/AvReckoner.app   - the app bundle
#   dist_mac/AvReckoner.dmg   - drag-to-Applications disk image to hand out
#
# NOTE: this build is ad-hoc signed only (not a paid Apple Developer ID),
# so recipients will see a Gatekeeper warning on first launch. They need to
# right-click the app -> Open (once) instead of double-clicking, or allow
# it via System Settings -> Privacy & Security -> "Open Anyway".
set -euo pipefail

cd "$(dirname "$0")"

APP_NAME="AvReckoner"
RID="osx-arm64"
PUBLISH_DIR="publish/$RID"
DIST_DIR="dist_mac"
APP_BUNDLE="$DIST_DIR/$APP_NAME.app"
FIPY_BIN="../../Tools/fiPy/dist/fipy"

echo "==> Publishing (Release, self-contained, $RID)..."
dotnet publish AvReckoner.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=false -o "$PUBLISH_DIR"

echo "==> Building app bundle..."
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS" "$APP_BUNDLE/Contents/Resources"

cp -R "$PUBLISH_DIR/." "$APP_BUNDLE/Contents/MacOS/"

if [ -f "$FIPY_BIN" ]; then
    cp "$FIPY_BIN" "$APP_BUNDLE/Contents/MacOS/fipy"
    chmod +x "$APP_BUNDLE/Contents/MacOS/fipy"
else
    echo "WARNING: $FIPY_BIN not found — build it first (Tools/fiPy/build_mac.sh). Packaging without it."
fi

chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

cat > "$APP_BUNDLE/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>com.mattdubose.avreckoner</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
</dict>
</plist>
PLIST

echo "==> Ad-hoc signing (required for the app to launch at all on Apple Silicon)..."
codesign --force --deep --sign - "$APP_BUNDLE"

echo "==> Building DMG..."
rm -f "$DIST_DIR/$APP_NAME.dmg"
create-dmg \
    --volname "$APP_NAME" \
    --window-size 500 300 \
    --icon-size 100 \
    --icon "$APP_NAME.app" 125 130 \
    --app-drop-link 375 130 \
    "$DIST_DIR/$APP_NAME.dmg" \
    "$APP_BUNDLE" \
    || true   # create-dmg exits non-zero on some benign AppleScript/Finder races; DMG is usually still produced

echo "==> Done."
ls -la "$DIST_DIR"
