const express = require('express');
const router = express.Router();
const BackgroundJobManager = require('../services/BackgroundJobManager');
const db = require('../database');
const Logger = require('../utils/logger');

// Generate cache for selected collections
router.post('/collections/generate-cache', async (req, res) => {
  const logger = new Logger('CacheGenerationController');
  
  try {
    const {
      collectionIds,
      quality,
      format,
      overwrite
    } = req.body;

    logger.flow('GENERATE_CACHE_REQUEST_START', {
      collectionIds,
      quality,
      format,
      overwrite
    });

    // If no collection IDs provided, generate for all collections
    let targetCollectionIds = collectionIds;
    if (!targetCollectionIds || !Array.isArray(targetCollectionIds) || targetCollectionIds.length === 0) {
      // Get all collection IDs
      logger.debug('No collection IDs provided, getting all collections');
      const allCollections = await db.getAllCollections();
      targetCollectionIds = allCollections.map(col => col.id);
      logger.info('Generating cache for ALL collections', {
        totalCollections: targetCollectionIds.length
      });
    }

    if (!quality || quality < 1 || quality > 100) {
      logger.warn('Invalid quality parameter', { quality });
      return res.status(400).json({ error: 'Quality must be between 1 and 100' });
    }

    // Validate collections exist
    logger.debug('Validating collections exist', { collectionIds: targetCollectionIds });
    const collections = await Promise.all(
      targetCollectionIds.map(id => db.getCollection(id))
    );

    const validCollections = collections.filter(Boolean);
    if (validCollections.length !== targetCollectionIds.length) {
      logger.error('Some collections not found', {
        requested: targetCollectionIds.length,
        found: validCollections.length,
        missing: targetCollectionIds.length - validCollections.length
      });
      return res.status(400).json({ error: 'One or more collections not found' });
    }

    logger.info('Collections validated successfully', {
      totalCollections: validCollections.length,
      collections: validCollections.map(c => ({ id: c.id, name: c.name, totalImages: c.settings?.total_images || 0 }))
    });

    // Start background job
    logger.debug('Creating background job', {
      type: 'cache-generation',
      collectionCount: targetCollectionIds.length,
      quality,
      format,
      overwrite
    });
    
    const jobId = BackgroundJobManager.createJob('cache-generation', {
      collectionIds: targetCollectionIds,
      quality,
      format,
      overwrite,
      collections: validCollections
    });

    logger.flow('GENERATE_CACHE_REQUEST_SUCCESS', {
      jobId,
      collectionCount: targetCollectionIds.length
    });

    res.json({ 
      jobId,
      message: `Cache generation started for ${targetCollectionIds.length} collections`
    });

  } catch (error) {
    logger.error('Error starting cache generation', {
      error: error.message,
      stack: error.stack
    });
    res.status(500).json({ error: 'Failed to start cache generation' });
  }
});

// Get cache generation status for a collection
router.get('/collections/:id/cache-status', async (req, res) => {
  const logger = new Logger('CacheStatusController');
  
  try {
    const { id } = req.params;
    
    logger.debug('Getting cache status', { collectionId: id });
    
    if (!id || id === 'undefined') {
      logger.warn('Missing collection ID');
      return res.status(400).json({ error: 'Collection ID is required' });
    }

    const cacheStatus = await db.getCollectionCacheStatus(id);
    logger.debug('Cache status retrieved', { 
      collectionId: id, 
      hasCache: cacheStatus.hasCache,
      cachedImages: cacheStatus.cachedImages,
      totalImages: cacheStatus.totalImages
    });
    
    res.json(cacheStatus);

  } catch (error) {
    logger.error('Error getting cache status', {
      collectionId: req.params.id,
      error: error.message,
      stack: error.stack
    });
    res.status(500).json({ error: 'Failed to get cache status' });
  }
});

// Clear cache for a collection
router.delete('/collections/:id/cache', async (req, res) => {
  const logger = new Logger('CacheClearController');
  
  try {
    const { id } = req.params;
    
    logger.debug('Clearing cache for collection', { collectionId: id });
    
    if (!id || id === 'undefined') {
      logger.warn('Missing collection ID for cache clear');
      return res.status(400).json({ error: 'Collection ID is required' });
    }

    await db.clearCollectionCache(id);
    logger.info('Cache cleared successfully', { collectionId: id });
    
    res.json({ message: 'Cache cleared successfully' });

  } catch (error) {
    logger.error('Error clearing cache', {
      collectionId: req.params.id,
      error: error.message,
      stack: error.stack
    });
    res.status(500).json({ error: 'Failed to clear cache' });
  }
});

// Get overall cache statistics
router.get('/cache/statistics', async (req, res) => {
  const logger = new Logger('CacheStatsController');
  
  try {
    logger.debug('Getting cache statistics');
    const stats = await db.getCacheStatistics();
    
    logger.debug('Cache statistics retrieved', {
      totalCollections: stats.totalCollections,
      cachedCollections: stats.cachedCollections,
      totalImages: stats.totalImages,
      cachedImages: stats.cachedImages
    });
    
    res.json(stats);

  } catch (error) {
    logger.error('Error getting cache statistics', {
      error: error.message,
      stack: error.stack
    });
    res.status(500).json({ error: 'Failed to get cache statistics' });
  }
});

module.exports = router;
