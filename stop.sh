#!/bin/bash

echo "Stopping Image Viewer Application..."

# Check if PM2 is installed globally
if command -v pm2 &> /dev/null; then
    echo "Using global PM2 installation..."
    pm2 stop image-viewer
else
    echo "PM2 not found globally, using npx..."
    npx pm2 stop image-viewer
fi

echo ""
echo "Application stopped successfully!"
echo ""
