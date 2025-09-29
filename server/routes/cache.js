const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const db = require('../database');

// Clear cache
router.delete('/', async (req, res) => {
  try {
    await db.clearExpiredCache();
    
    // Clear thumbnail cache
    const thumbnailDir = path.join(__dirname, '../cache/thumbnails');
    await fs.remove(thumbnailDir);
    await fs.ensureDir(thumbnailDir);
    
    res.json({ message: 'Cache cleared successfully' });
  } catch (error) {
    console.error('Error clearing cache:', error);
    res.status(500).json({ error: 'Failed to clear cache' });
  }
});

// Get cache statistics
router.get('/stats', async (req, res) => {
  try {
    // Get thumbnail directory size
    const thumbnailDir = path.join(__dirname, '../cache/thumbnails');
    let thumbnailSize = 0;
    let thumbnailCount = 0;
    
    if (await fs.pathExists(thumbnailDir)) {
      const files = await fs.readdir(thumbnailDir, { recursive: true });
      for (const file of files) {
        const filePath = path.join(thumbnailDir, file);
        const stats = await fs.stat(filePath);
        if (stats.isFile()) {
          thumbnailSize += stats.size;
          thumbnailCount++;
        }
      }
    }
    
    res.json({
      thumbnails: {
        count: thumbnailCount,
        size: thumbnailSize,
        sizeFormatted: formatBytes(thumbnailSize)
      }
    });
  } catch (error) {
    console.error('Error getting cache stats:', error);
    res.status(500).json({ error: 'Failed to get cache statistics' });
  }
});

// Helper function to format bytes
function formatBytes(bytes, decimals = 2) {
  if (bytes === 0) return '0 Bytes';
  
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
  
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

module.exports = router;
