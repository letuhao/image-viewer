const express = require('express');
const router = express.Router();
const db = require('../database');
const collectionThumbnailService = require('../services/collectionThumbnailService');
const jobManager = require('../services/backgroundJobs');

// Batch regenerate thumbnails for multiple collections
router.post('/batch-regenerate-thumbnails', async (req, res) => {
  try {
    const { collectionIds } = req.body;
    
    if (!collectionIds || !Array.isArray(collectionIds) || collectionIds.length === 0) {
      return res.status(400).json({ error: 'Collection IDs array is required' });
    }

    // Validate collections exist
    const collections = await Promise.all(
      collectionIds.map(async (id) => {
        const collection = await db.getCollection(id);
        if (!collection) {
          throw new Error(`Collection ${id} not found`);
        }
        return collection;
      })
    );

    // Create background job
    const jobManager = new BackgroundJobManager();
    const jobId = await jobManager.createJob('batch-regenerate-thumbnails', {
      collectionIds,
      total: collectionIds.length
    });

    // Start job in background
    jobManager.performJob(jobId, async (progressCallback) => {
      const results = await collectionThumbnailService.batchRegenerateThumbnails(
        collectionIds,
        progressCallback
      );
      
      return {
        results,
        summary: {
          total: results.total,
          success: results.success,
          failed: results.failed,
          errors: results.errors
        }
      };
    });

    res.json({
      jobId,
      message: `Started regenerating thumbnails for ${collectionIds.length} collections`,
      total: collectionIds.length
    });

  } catch (error) {
    console.error('Error starting batch regenerate job:', error);
    res.status(500).json({ error: 'Failed to start regenerate job' });
  }
});

// Regenerate single collection thumbnail
router.post('/:collectionId/regenerate', async (req, res) => {
  try {
    const { collectionId } = req.params;
    const collection = await db.getCollection(collectionId);
    
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }

    // Generate thumbnail
    const thumbnailPath = await collectionThumbnailService.regenerateCollectionThumbnail(collectionId);
    
    if (thumbnailPath) {
      res.json({ 
        message: 'Thumbnail regenerated successfully',
        thumbnail_url: `/api/collections/${collectionId}/thumbnail`
      });
    } else {
      res.status(400).json({ error: 'Failed to generate thumbnail' });
    }
  } catch (error) {
    console.error('Error regenerating thumbnail:', error);
    res.status(500).json({ error: 'Failed to regenerate thumbnail' });
  }
});

// Generate thumbnails for ALL collections
router.post('/generate-all-thumbnails', async (req, res) => {
  try {
    console.log('[DEBUG] Generate all thumbnails endpoint called');
    
    // Check if a job is already running
    if (jobManager.hasRunningJob('generate-all-thumbnails')) {
      console.log('[DEBUG] Job already running, returning 409');
      return res.status(409).json({ error: 'A generate all thumbnails job is already running.' });
    }

    // Get all collections
    console.log('[DEBUG] Getting all collections from database');
    const collections = await db.getAllCollections();
    const collectionIds = collections.map(col => col.id);
    console.log(`[DEBUG] Found ${collections.length} collections`);

    if (collectionIds.length === 0) {
      console.log('[DEBUG] No collections found');
      return res.status(400).json({ error: 'No collections found' });
    }

    console.log('[DEBUG] Creating job with jobManager');
    const jobId = jobManager.createJob('generate-all-thumbnails', collectionIds.length);
    console.log(`[DEBUG] Job created with ID: ${jobId}`);

    // Start the background job
    console.log('[DEBUG] Starting background job');
    jobManager.startJob(jobId, async (updateProgress) => {
      console.log(`[DEBUG] Background job ${jobId} started processing`);
      for (let i = 0; i < collectionIds.length; i++) {
        if (jobManager.isJobCancelled(jobId)) {
          console.log(`[DEBUG] Job ${jobId} was cancelled`);
          updateProgress(i, collectionIds.length, 'Cancelled');
          return;
        }

        const collectionId = collectionIds[i];
        console.log(`[DEBUG] Processing collection ${i + 1}/${collectionIds.length}: ${collectionId}`);
        updateProgress(i + 1, collectionIds.length, `Generating thumbnail for collection ${i + 1}/${collectionIds.length}`);

        try {
          await collectionThumbnailService.regenerateCollectionThumbnail(collectionId);
          console.log(`[DEBUG] Successfully processed collection ${collectionId}`);
        } catch (error) {
          console.error(`[DEBUG] Error generating thumbnail for collection ${collectionId}:`, error);
          // Continue with other collections even if one fails
        }
      }
      console.log(`[DEBUG] Background job ${jobId} completed`);
    });

    console.log(`[DEBUG] Returning success response with jobId: ${jobId}`);
    res.json({ message: 'Generate all thumbnails job started', jobId });
  } catch (error) {
    console.error('[DEBUG] Error starting generate all thumbnails job:', error);
    res.status(500).json({ error: 'Failed to start generate all thumbnails job' });
  }
});

// Get thumbnail regenerate statistics
router.get('/stats', async (req, res) => {
  try {
    const collections = await db.getAllCollections();
    
    const stats = {
      total: collections.length,
      withThumbnails: 0,
      withoutThumbnails: 0,
      recentlyGenerated: 0
    };

    const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);

    for (const collection of collections) {
      if (collection.thumbnail_url) {
        stats.withThumbnails++;
        
        if (collection.thumbnail_generated_at && new Date(collection.thumbnail_generated_at) > oneDayAgo) {
          stats.recentlyGenerated++;
        }
      } else {
        stats.withoutThumbnails++;
      }
    }

    res.json(stats);
  } catch (error) {
    console.error('Error getting thumbnail stats:', error);
    res.status(500).json({ error: 'Failed to get thumbnail statistics' });
  }
});

module.exports = router;
