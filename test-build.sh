#!/bin/bash
set -e

echo "=== Testing Release Build ==="
dotnet build -c Release src/Santy.Web/Santy.Web.csproj

echo ""
echo "=== Testing macOS Publish ==="
dotnet publish -c Release \
  -o dist-mac-test \
  --self-contained \
  --runtime osx-arm64 \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  src/Santy.Web

echo ""
echo "✅ Build erfolgreich!"
ls -lh dist-mac-test/ | head -10
