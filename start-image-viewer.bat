@echo off
cd /d "D:\Works\source\image-viewer"
pm2 start ecosystem.config.js
echo Image Viewer started with PM2 on http://localhost:10001
pause

