const express = require('express');
const router = express.Router();
const jobManager = require('../services/backgroundJobs');
const { findAllCollections, generateCollectionMetadata } = require('./bulk');
const db = require('../database');
const path = require('path');
const Logger = require('../utils/logger');

// Start background bulk add job
router.post('/bulk-add/start', async (req, res) => {
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }

    // Create background job
    const jobData = {
      parentPath,
      collectionPrefix,
      includeSubfolders
    };
    
    const logger = new Logger('BulkAddJob');
    logger.info('Creating job with data', jobData);
    const jobId = jobManager.createJob('bulk_add_collections', jobData);
    logger.info('Job created successfully', { jobId });

    // Start the job
    jobManager.startJob(jobId, async (updateProgress) => {
      logger.info('Starting job execution', { jobId });
      return await performBulkAdd(jobId, updateProgress, logger);
    });

    res.json({
      success: true,
      jobId,
      message: 'Bulk add job started in background'
    });

  } catch (error) {
    const logger = new Logger('BulkAddJob');
    logger.error('Error starting bulk add job', { 
      error: error.message, 
      stack: error.stack 
    });
    res.status(500).json({ error: 'Failed to start bulk add job' });
  }
});

// Get job status
router.get('/jobs/:jobId', (req, res) => {
  try {
    const { jobId } = req.params;
    const logger = new Logger('BulkAddJob');
    logger.debug('Getting job status', { jobId });
    logger.debug('Job manager info', { 
      jobManagerType: typeof jobManager,
      getJobType: typeof jobManager.getJob 
    });
    
    const job = jobManager.getJob(jobId);
    logger.debug('Job lookup result', { jobId, found: !!job });
    
    if (!job) {
      logger.warn('Job not found', { jobId });
      return res.status(404).json({ error: 'Job not found' });
    }

    logger.info('Job found, returning data', { jobId });
    res.json({
      success: true,
      job
    });

  } catch (error) {
    const logger = new Logger('BulkAddJob');
    logger.error('Error getting job status', { 
      jobId: req.params.jobId,
      error: error.message, 
      stack: error.stack 
    });
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
async function performBulkAdd(jobId, updateProgress, logger) {
  logger.flow('PERFORM_BULK_ADD_START', { jobId });
  
  // Get job data from jobManager
  const job = jobManager.getJob(jobId);
  if (!job || !job.data) {
    logger.error('Job or job data not found', { jobId, jobExists: !!job });
    throw new Error('Job or job data not found');
  }
  
  const { parentPath, collectionPrefix, includeSubfolders } = job.data;
  
  try {
    logger.info('Starting bulk add job', { 
      jobId, 
      parentPath, 
      collectionPrefix, 
      includeSubfolders 
    });
    
    // Update progress: Finding collections
    updateProgress(0, 0, 'Scanning directory for collections...');
    
    // Get all potential collections
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    logger.info('Found potential collections', { 
      jobId, 
      collectionCount: allCollections.length 
    });
    
    if (allCollections.length === 0) {
      logger.warn('No collections found to add', { jobId, parentPath });
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
    updateProgress(0, allCollections.length, `Processing ${allCollections.length} collections...`);
    
    const collections = [];
    const errors = [];
    
    for (let i = 0; i < allCollections.length; i++) {
      const collectionInfo = allCollections[i];
      
      try {
        // Update progress
        updateProgress(i + 1, allCollections.length, `Processing: ${collectionInfo.name}`);
        
        logger.debug('Processing collection', { 
          jobId,
          current: i + 1, 
          total: allCollections.length, 
          collectionName: collectionInfo.name 
        });
        
        // Check if collection already exists
        const existingCollections = await db.getCollections();
        const alreadyExists = existingCollections.some(col => 
          col.path === collectionInfo.path || col.name === collectionInfo.name
        );
        
        if (!alreadyExists) {
          // Generate metadata for the collection
          logger.debug('Generating metadata for collection', { 
            jobId, 
            collectionName: collectionInfo.name 
          });
          const metadata = await generateCollectionMetadata(collectionInfo.path, collectionInfo.type);
          
          logger.info('Adding collection to database', { 
            jobId, 
            collectionName: collectionInfo.name 
          });
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
          
          logger.info('Successfully added collection', { 
            jobId, 
            collectionName: collectionInfo.name, 
            collectionId 
          });
        } else {
          logger.debug('Collection already exists, skipping', { 
            jobId, 
            collectionName: collectionInfo.name 
          });
        }
      } catch (error) {
        logger.error('Error processing collection', { 
          jobId, 
          collectionName: collectionInfo.name, 
          error: error.message, 
          stack: error.stack 
        });
        errors.push({
          item: collectionInfo.name,
          error: error.message
        });
      }
    }

    // Update progress: Completed
    updateProgress(allCollections.length, allCollections.length, 'Bulk add completed');
    
    const result = {
      collections,
      errors,
      total: allCollections.length,
      added: collections.length,
      skipped: allCollections.length - collections.length - errors.length,
      message: `Successfully added ${collections.length} collections`
    };

    logger.info('Job completed successfully', { 
      jobId, 
      result 
    });
    logger.flow('PERFORM_BULK_ADD_END', { jobId, result });
    return result;

  } catch (error) {
    logger.error('Job failed', { 
      jobId, 
      error: error.message, 
      stack: error.stack 
    });
    logger.flow('PERFORM_BULK_ADD_ERROR', { jobId, error: error.message });
    throw error;
  }
}

module.exports = router;
