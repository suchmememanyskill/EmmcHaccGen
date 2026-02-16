#!/bin/bash
# build script for EmmcHaccGen with SD Card Preparation
# creates single-file executables for Windows, Linux, and macOS
echo "======================================"
echo "Building EmmcHaccGen Releases"
echo "======================================"
echo ""
PROJECT="EmmcHaccGen.GUI/EmmcHaccGen.GUI.csproj"
OUTPUT_DIR="releases"
# clean old releases
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning old releases..."
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"
# build for Windows (x64)
echo ""
echo "Building for Windows x64..."
dotnet publish "$PROJECT" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT_DIR/win-x64"
if [ $? -eq 0 ]; then
    echo "✓ Windows build complete"
    cd "$OUTPUT_DIR/win-x64"
    mv EmmcHaccGen.GUI.exe EmmcHaccGen-win-x64.GUI.exe
    zip -q ../EmmcHaccGen-win-x64.zip EmmcHaccGen-win-x64.GUI.exe
    cd ../..
    echo "✓ Created: EmmcHaccGen-win-x64.zip"
else
    echo "✗ Windows build failed"
fi
# build for Linux (x64)
echo ""
echo "Building for Linux x64..."
dotnet publish "$PROJECT" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT_DIR/linux-x64"
if [ $? -eq 0 ]; then
    echo "✓ Linux build complete"
    cd "$OUTPUT_DIR/linux-x64"
    mv EmmcHaccGen.GUI EmmcHaccGen-linux-x64.GUI
    chmod +x EmmcHaccGen-linux-x64.GUI
    tar -czf ../EmmcHaccGen-linux-x64.tar.gz EmmcHaccGen-linux-x64.GUI
    cd ../..
    echo "✓ Created: EmmcHaccGen-linux-x64.tar.gz"
else
    echo "✗ Linux build failed"
fi
# build for macOS (x64)
echo ""
echo "Building for macOS x64..."
dotnet publish "$PROJECT" \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT_DIR/osx-x64"
if [ $? -eq 0 ]; then
    echo "✓ macOS build complete"
    cd "$OUTPUT_DIR/osx-x64"
    mv EmmcHaccGen.GUI EmmcHaccGen-osx-x64.GUI
    chmod +x EmmcHaccGen-osx-x64.GUI
    
    # create .app bundle
    echo "Creating .app bundle..."
    APP_NAME="EmmcHaccGen"
    APP_BUNDLE="${APP_NAME}.app"
    
    mkdir -p "${APP_BUNDLE}/Contents/MacOS"
    mkdir -p "${APP_BUNDLE}/Contents/Resources"
    
    cp EmmcHaccGen-osx-x64.GUI "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    chmod +x "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    
    cat > "${APP_BUNDLE}/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs-propertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>EmmcHaccGen</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.emmchaccgen.extended</string>
    <key>CFBundleName</key>
    <string>EmmcHaccGen Extended</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>3.2.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
</dict>
<-plist>
EOF

if [ -f "../../EmmcHaccGen.GUI/Assets/logo.icns" ]; then
    cp ../../EmmcHaccGen.GUI/Assets/logo.icns "${APP_BUNDLE}/Contents/Resources/AppIcon.icns"
    echo "✓ Added icon to .app bundle"
else
    echo "⚠ icon.icns not found, .app will use default icon"
fi
    
    echo "✓ Created .app bundle"
    zip -qr ../EmmcHaccGen-osx-x64-app.zip "${APP_BUNDLE}"
    tar -czf ../EmmcHaccGen-osx-x64.tar.gz EmmcHaccGen-osx-x64.GUI
    cd ../..
    echo "✓ Created: EmmcHaccGen-osx-x64.tar.gz"
    echo "✓ Created: EmmcHaccGen-osx-x64-app.zip (.app bundle)"
else
    echo "✗ macOS build failed"
fi
# build for macOS (ARM64)
echo ""
echo "Building for macOS ARM64 (Apple Silicon)..."
dotnet publish "$PROJECT" \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT_DIR/osx-arm64"
if [ $? -eq 0 ]; then
    echo "✓ macOS ARM64 build complete"
    cd "$OUTPUT_DIR/osx-arm64"
    mv EmmcHaccGen.GUI EmmcHaccGen-osx-arm64.GUI
    chmod +x EmmcHaccGen-osx-arm64.GUI
    
    # create .app bundle
    echo "Creating .app bundle..."
    APP_NAME="EmmcHaccGen"
    APP_BUNDLE="${APP_NAME}.app"
    
    mkdir -p "${APP_BUNDLE}/Contents/MacOS"
    mkdir -p "${APP_BUNDLE}/Contents/Resources"
    
    cp EmmcHaccGen-osx-arm64.GUI "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    chmod +x "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    
    cat > "${APP_BUNDLE}/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs-propertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>EmmcHaccGen</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.emmchaccgen.extended</string>
    <key>CFBundleName</key>
    <string>EmmcHaccGen Extended</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>3.2.1</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
</dict>
<-plist>
EOF

if [ -f "../../EmmcHaccGen.GUI/Assets/logo.icns" ]; then
    cp ../../EmmcHaccGen.GUI/Assets/logo.icns "${APP_BUNDLE}/Contents/Resources/AppIcon.icns"
    echo "✓ Added icon to .app bundle"
else
    echo "⚠ icon.icns not found, .app will use default icon"
fi

    echo "✓ Created .app bundle"
    zip -qr ../EmmcHaccGen-osx-arm64-app.zip "${APP_BUNDLE}"
    tar -czf ../EmmcHaccGen-osx-arm64.tar.gz EmmcHaccGen-osx-arm64.GUI
    cd ../..
    echo "✓ Created: EmmcHaccGen-osx-arm64.tar.gz"
    echo "✓ Created: EmmcHaccGen-osx-arm64-app.zip (.app bundle)"
else
    echo "✗ macOS ARM64 build failed"
fi
echo ""
echo "======================================"
echo "Build Summary"
echo "======================================"
echo ""
echo "Release files in: $OUTPUT_DIR/"
ls -lh "$OUTPUT_DIR"/*.zip "$OUTPUT_DIR"/*.tar.gz 2>/dev/null
echo ""
echo "Single executables:"
echo "  Windows:      releases/win-x64/EmmcHaccGen-win-x64.GUI.exe"
echo "  Linux:        releases/linux-x64/EmmcHaccGen-linux-x64.GUI"
echo "  macOS (x64):  releases/osx-x64/EmmcHaccGen-osx-x64.GUI"
echo "  macOS (ARM):  releases/osx-arm64/EmmcHaccGen-osx-arm64.GUI"
echo ""
echo "macOS .app bundles:"
echo "  macOS (x64):  releases/osx-x64/EmmcHaccGen.app"
echo "  macOS (ARM):  releases/osx-arm64/EmmcHaccGen.app"
echo ""
echo "Archives for distribution:"
echo "  - EmmcHaccGen-win-x64.zip"
echo "  - EmmcHaccGen-linux-x64.tar.gz"
echo "  - EmmcHaccGen-osx-x64.tar.gz (CLI version)"
echo "  - EmmcHaccGen-osx-x64-app.zip (.app bundle)"
echo "  - EmmcHaccGen-osx-arm64.tar.gz (CLI version)"
echo "  - EmmcHaccGen-osx-arm64-app.zip (.app bundle)"
echo ""
echo "Done!"