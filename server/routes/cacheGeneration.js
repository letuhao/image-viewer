const express = require('express');
const router = express.Router();
const BackgroundJobManager = require('../services/BackgroundJobManager');
const db = require('../database');

// Generate cache for selected collections
router.post('/collections/generate-cache', async (req, res) => {
  try {
    const {
      collectionIds,
      quality,
      format,
      overwrite
    } = req.body;

    // If no collection IDs provided, generate for all collections
    let targetCollectionIds = collectionIds;
    if (!targetCollectionIds || !Array.isArray(targetCollectionIds) || targetCollectionIds.length === 0) {
      // Get all collection IDs
      const allCollections = await db.getAllCollections();
      targetCollectionIds = allCollections.map(col => col.id);
      console.log(`[CACHE-GEN] Generating cache for ALL ${targetCollectionIds.length} collections`);
    }

    if (!quality || quality < 1 || quality > 100) {
      return res.status(400).json({ error: 'Quality must be between 1 and 100' });
    }

    // Validate collections exist
    const collections = await Promise.all(
      targetCollectionIds.map(id => db.getCollection(id))
    );

    const validCollections = collections.filter(Boolean);
    if (validCollections.length !== targetCollectionIds.length) {
      return res.status(400).json({ error: 'One or more collections not found' });
    }

    // Start background job
    const jobId = BackgroundJobManager.createJob('cache-generation', {
      collectionIds: targetCollectionIds,
      quality,
      format,
      overwrite,
      collections: validCollections
    });

    res.json({ 
      jobId,
      message: `Cache generation started for ${targetCollectionIds.length} collections`
    });

  } catch (error) {
    console.error('Error starting cache generation:', error);
    res.status(500).json({ error: 'Failed to start cache generation' });
  }
});

// Get cache generation status for a collection
router.get('/collections/:id/cache-status', async (req, res) => {
  try {
    const { id } = req.params;
    
    if (!id || id === 'undefined') {
      return res.status(400).json({ error: 'Collection ID is required' });
    }

    const cacheStatus = await db.getCollectionCacheStatus(id);
    res.json(cacheStatus);

  } catch (error) {
    console.error('Error getting cache status:', error);
    res.status(500).json({ error: 'Failed to get cache status' });
  }
});

// Clear cache for a collection
router.delete('/collections/:id/cache', async (req, res) => {
  try {
    const { id } = req.params;
    
    if (!id || id === 'undefined') {
      return res.status(400).json({ error: 'Collection ID is required' });
    }

    await db.clearCollectionCache(id);
    res.json({ message: 'Cache cleared successfully' });

  } catch (error) {
    console.error('Error clearing cache:', error);
    res.status(500).json({ error: 'Failed to clear cache' });
  }
});

// Get overall cache statistics
router.get('/cache/statistics', async (req, res) => {
  try {
    const stats = await db.getCacheStatistics();
    res.json(stats);

  } catch (error) {
    console.error('Error getting cache statistics:', error);
    res.status(500).json({ error: 'Failed to get cache statistics' });
  }
});

module.exports = router;
