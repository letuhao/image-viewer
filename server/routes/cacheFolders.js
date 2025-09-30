const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const db = require('../database');

// Get all cache folders
router.get('/', async (req, res) => {
  try {
    const stats = await db.getCacheFolderStats();
    res.json(stats);
  } catch (error) {
    console.error('Error fetching cache folders:', error);
    res.status(500).json({ error: 'Failed to fetch cache folders' });
  }
});

// Add new cache folder
router.post('/', async (req, res) => {
  try {
    const { name, path: folderPath, priority = 0, maxSize = null } = req.body;
    
    if (!name || !folderPath) {
      return res.status(400).json({ error: 'Name and path are required' });
    }

    // Validate path exists and is writable
    try {
      await fs.ensureDir(folderPath);
      await fs.access(folderPath, fs.constants.W_OK);
    } catch (error) {
      return res.status(400).json({ error: 'Invalid path or insufficient permissions' });
    }

    const cacheFolderId = await db.addCacheFolder(name, folderPath, priority, maxSize);
    
    res.json({ 
      success: true, 
      id: cacheFolderId,
      message: 'Cache folder added successfully' 
    });
  } catch (error) {
    console.error('Error adding cache folder:', error);
    if (error.message.includes('already exists')) {
      res.status(409).json({ error: error.message });
    } else {
      res.status(500).json({ error: 'Failed to add cache folder' });
    }
  }
});

// Update cache folder
router.put('/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const updates = req.body;
    
    // Validate ObjectId format
    if (!id || id.length !== 24) {
      return res.status(400).json({ error: 'Invalid cache folder ID format' });
    }
    
    // If path is being updated, validate it
    if (updates.path) {
      try {
        await fs.ensureDir(updates.path);
        await fs.access(updates.path, fs.constants.W_OK);
      } catch (error) {
        return res.status(400).json({ error: 'Invalid path or insufficient permissions' });
      }
    }

    const success = await db.updateCacheFolder(id, updates);
    
    if (success) {
      res.json({ success: true, message: 'Cache folder updated successfully' });
    } else {
      res.status(404).json({ error: 'Cache folder not found' });
    }
  } catch (error) {
    console.error('Error updating cache folder:', error);
    if (error.message.includes('Invalid cache folder ID format')) {
      res.status(400).json({ error: error.message });
    } else {
      res.status(500).json({ error: 'Failed to update cache folder' });
    }
  }
});

// Delete cache folder
router.delete('/:id', async (req, res) => {
  try {
    const { id } = req.params;
    
    // Validate ObjectId format
    if (!id || id.length !== 24) {
      return res.status(400).json({ error: 'Invalid cache folder ID format' });
    }
    
    const success = await db.deleteCacheFolder(id);
    
    if (success) {
      res.json({ success: true, message: 'Cache folder deleted successfully' });
    } else {
      res.status(404).json({ error: 'Cache folder not found' });
    }
  } catch (error) {
    console.error('Error deleting cache folder:', error);
    if (error.message.includes('Invalid cache folder ID format')) {
      res.status(400).json({ error: error.message });
    } else {
      res.status(500).json({ error: 'Failed to delete cache folder' });
    }
  }
});

// Get cache folder for a specific collection
router.get('/collection/:collectionId', async (req, res) => {
  try {
    const { collectionId } = req.params;
    const cacheFolder = await db.getCollectionCacheFolder(collectionId);
    
    if (cacheFolder) {
      res.json(cacheFolder);
    } else {
      res.status(404).json({ error: 'No cache folder assigned to this collection' });
    }
  } catch (error) {
    console.error('Error fetching collection cache folder:', error);
    res.status(500).json({ error: 'Failed to fetch collection cache folder' });
  }
});

// Assign cache folder to collection
router.post('/:id/bind/:collectionId', async (req, res) => {
  try {
    const { id: cacheFolderId, collectionId } = req.params;
    const success = await db.bindCollectionToCacheFolder(collectionId, cacheFolderId);
    
    if (success) {
      res.json({ success: true, message: 'Cache folder bound to collection successfully' });
    } else {
      res.status(400).json({ error: 'Failed to bind cache folder to collection' });
    }
  } catch (error) {
    console.error('Error binding cache folder to collection:', error);
    res.status(500).json({ error: 'Failed to bind cache folder to collection' });
  }
});

// Get cache folder usage statistics
router.get('/:id/stats', async (req, res) => {
  try {
    const { id } = req.params;
    const stats = await db.getCacheFolderStats();
    
    const folder = stats.folders.find(f => f.id === id);
    if (folder) {
      res.json(folder);
    } else {
      res.status(404).json({ error: 'Cache folder not found' });
    }
  } catch (error) {
    console.error('Error fetching cache folder stats:', error);
    res.status(500).json({ error: 'Failed to fetch cache folder statistics' });
  }
});

// Validate cache folder path
router.post('/validate', async (req, res) => {
  try {
    const { path: folderPath } = req.body;
    
    if (!folderPath) {
      return res.status(400).json({ error: 'Path is required' });
    }

    try {
      await fs.ensureDir(folderPath);
      await fs.access(folderPath, fs.constants.W_OK);
      
      // Get disk space info
      const stats = await fs.stat(folderPath);
      const freeSpace = await fs.statvfs ? await fs.statvfs(folderPath) : null;
      
      res.json({
        valid: true,
        exists: true,
        writable: true,
        path: folderPath,
        stats: {
          isDirectory: stats.isDirectory(),
          size: stats.size,
          freeSpace: freeSpace ? freeSpace.bavail * freeSpace.bsize : null
        }
      });
    } catch (error) {
      res.json({
        valid: false,
        exists: false,
        writable: false,
        path: folderPath,
        error: error.message
      });
    }
  } catch (error) {
    console.error('Error validating cache folder path:', error);
    res.status(500).json({ error: 'Failed to validate cache folder path' });
  }
});

module.exports = router;
