#!/bin/bash

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Set default PORT if not defined
export PORT=${PORT:-10001}

echo "Starting Image Viewer Application..."

# Check if PM2 is installed globally
if command -v pm2 &> /dev/null; then
    echo "Using global PM2 installation..."
    pm2 start ecosystem.config.js
else
    echo "PM2 not found globally, using npx..."
    npx pm2 start ecosystem.config.js
fi

echo ""
echo "Application started successfully!"
echo ""
echo "Backend API: http://localhost:${PORT:-10001}"
echo "Frontend: http://localhost:4000 (in development mode)"
echo ""
echo "To view logs: npx pm2 logs image-viewer"
echo "To stop: npx pm2 stop image-viewer"
echo "To restart: npx pm2 restart image-viewer"
echo "To check status: npx pm2 status"
echo ""
