const express = require('express');
const router = express.Router();
const jobManager = require('../services/backgroundJobs');
const { findAllCollections, generateCollectionMetadata } = require('./bulk');
const db = require('../database');
const path = require('path');

// Start background bulk add job
router.post('/bulk-add/start', async (req, res) => {
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }

    // Create background job
    const jobId = jobManager.createJob('bulk_add_collections', {
      parentPath,
      collectionPrefix,
      includeSubfolders
    });

    // Start the job
    jobManager.startJob(jobId, async (job, manager) => {
      return await performBulkAdd(job, manager);
    });

    res.json({
      success: true,
      jobId,
      message: 'Bulk add job started in background'
    });

  } catch (error) {
    console.error('Error starting bulk add job:', error);
    res.status(500).json({ error: 'Failed to start bulk add job' });
  }
});

// Get job status
router.get('/jobs/:jobId', (req, res) => {
  try {
    const { jobId } = req.params;
    const job = jobManager.getJob(jobId);
    
    if (!job) {
      return res.status(404).json({ error: 'Job not found' });
    }

    res.json({
      success: true,
      job
    });

  } catch (error) {
    console.error('Error getting job status:', error);
    res.status(500).json({ error: 'Failed to get job status' });
  }
});

// Get all jobs
router.get('/jobs', (req, res) => {
  try {
    const { status } = req.query;
    
    let jobs;
    if (status) {
      jobs = jobManager.getJobsByStatus(status);
    } else {
      jobs = jobManager.getAllJobs();
    }

    res.json({
      success: true,
      jobs
    });

  } catch (error) {
    console.error('Error getting jobs:', error);
    res.status(500).json({ error: 'Failed to get jobs' });
  }
});

// Cancel job
router.post('/jobs/:jobId/cancel', (req, res) => {
  try {
    const { jobId } = req.params;
    const cancelled = jobManager.cancelJob(jobId);
    
    if (!cancelled) {
      return res.status(400).json({ error: 'Job cannot be cancelled' });
    }

    res.json({
      success: true,
      message: 'Job cancelled successfully'
    });

  } catch (error) {
    console.error('Error cancelling job:', error);
    res.status(500).json({ error: 'Failed to cancel job' });
  }
});

// Background bulk add worker function
async function performBulkAdd(job, manager) {
  const { parentPath, collectionPrefix, includeSubfolders } = job.data;
  
  try {
    console.log(`[BACKGROUND BULK ADD] Starting job ${job.id} for path: ${parentPath}`);
    
    // Update progress: Finding collections
    manager.updateJobProgress(job.id, 5, 0, 0, 'Scanning directory for collections...');
    
    // Get all potential collections
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    console.log(`[BACKGROUND BULK ADD] Found ${allCollections.length} potential collections`);
    
    if (allCollections.length === 0) {
      return {
        collections: [],
        errors: [],
        total: 0,
        added: 0,
        skipped: 0,
        message: 'No collections found to add'
      };
    }

    // Update progress: Processing collections
    manager.updateJobProgress(job.id, 10, 0, allCollections.length, `Processing ${allCollections.length} collections...`);
    
    const collections = [];
    const errors = [];
    
    for (let i = 0; i < allCollections.length; i++) {
      const collectionInfo = allCollections[i];
      
      try {
        // Update progress
        const progress = 10 + (i / allCollections.length) * 80; // 10% to 90%
        manager.updateJobProgress(
          job.id, 
          progress, 
          i + 1, 
          allCollections.length, 
          `Processing: ${collectionInfo.name}`
        );
        
        console.log(`[BACKGROUND BULK ADD] Processing ${i + 1}/${allCollections.length}: ${collectionInfo.name}`);
        
        // Check if collection already exists
        const existingCollections = await db.getCollections();
        const alreadyExists = existingCollections.some(col => 
          col.path === collectionInfo.path || col.name === collectionInfo.name
        );
        
        if (!alreadyExists) {
          // Generate metadata for the collection
          console.log(`[BACKGROUND BULK ADD] Generating metadata for: ${collectionInfo.name}`);
          const metadata = await generateCollectionMetadata(collectionInfo.path, collectionInfo.type);
          
          console.log(`[BACKGROUND BULK ADD] Adding collection to database: ${collectionInfo.name}`);
          const collectionId = await db.addCollection(
            collectionInfo.name, 
            collectionInfo.path, 
            collectionInfo.type,
            metadata
          );
          
          collections.push({
            id: collectionId,
            name: collectionInfo.name,
            path: collectionInfo.path,
            type: collectionInfo.type,
            metadata
          });
          
          console.log(`[BACKGROUND BULK ADD] Successfully added collection: ${collectionInfo.name} (ID: ${collectionId})`);
        } else {
          console.log(`[BACKGROUND BULK ADD] Collection already exists, skipping: ${collectionInfo.name}`);
        }
      } catch (error) {
        console.error(`[BACKGROUND BULK ADD] Error processing collection ${collectionInfo.name}:`, error);
        errors.push({
          item: collectionInfo.name,
          error: error.message
        });
      }
    }

    // Update progress: Completed
    manager.updateJobProgress(job.id, 100, allCollections.length, allCollections.length, 'Bulk add completed');
    
    const result = {
      collections,
      errors,
      total: allCollections.length,
      added: collections.length,
      skipped: allCollections.length - collections.length - errors.length,
      message: `Successfully added ${collections.length} collections`
    };

    console.log(`[BACKGROUND BULK ADD] Job ${job.id} completed:`, result);
    return result;

  } catch (error) {
    console.error(`[BACKGROUND BULK ADD] Job ${job.id} failed:`, error);
    throw error;
  }
}

module.exports = router;
