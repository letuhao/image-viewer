const express = require('express');
const router = express.Router();
const BackgroundJobManager = require('../services/BackgroundJobManager');

// Get all jobs
router.get('/', async (req, res) => {
  try {
    const jobs = BackgroundJobManager.getAllJobs();
    console.log(`[DEBUG] All jobs:`, jobs);
    res.json(jobs);
    
  } catch (error) {
    console.error('Error getting all jobs:', error);
    res.status(500).json({ error: 'Failed to get jobs' });
  }
});

// Debug endpoint to check job manager state
router.get('/status', async (req, res) => {
  try {
    const jobs = BackgroundJobManager.getAllJobs();
    res.json({
      totalJobs: jobs.length,
      jobs: jobs,
      jobManagerType: typeof BackgroundJobManager,
      hasGetJob: typeof BackgroundJobManager.getJob === 'function'
    });
    
  } catch (error) {
    console.error('Error in debug endpoint:', error);
    res.status(500).json({ error: 'Debug failed' });
  }
});

// Get job progress
router.get('/:jobId', async (req, res) => {
  try {
    const { jobId } = req.params;
    
    console.log(`[DEBUG] Getting job status for jobId: ${jobId}`);
    console.log(`[DEBUG] jobManager type:`, typeof BackgroundJobManager);
    console.log(`[DEBUG] jobManager.getJob type:`, typeof BackgroundJobManager.getJob);
    
    const job = BackgroundJobManager.getJob(jobId);
    console.log(`[DEBUG] Job found:`, job ? 'Yes' : 'No');
    
    if (!job) {
      console.log(`[DEBUG] Job ${jobId} not found, returning 404`);
      return res.status(404).json({ error: 'Job not found' });
    }
    
    const progress = BackgroundJobManager.getJobProgress(jobId);
    console.log(`[DEBUG] Job progress:`, progress);
    res.json(progress);
    
  } catch (error) {
    console.error('Error getting job progress:', error);
    res.status(500).json({ error: 'Failed to get job progress' });
  }
});

// Cancel job
router.post('/:jobId/cancel', async (req, res) => {
  try {
    const { jobId } = req.params;
    
    const success = BackgroundJobManager.cancelJob(jobId);
    if (!success) {
      return res.status(404).json({ error: 'Job not found' });
    }
    
    res.json({ message: 'Job cancelled successfully' });
    
  } catch (error) {
    console.error('Error cancelling job:', error);
    res.status(500).json({ error: 'Failed to cancel job' });
  }
});

// Get all jobs
router.get('/', async (req, res) => {
  try {
    const jobs = BackgroundJobManager.getAllJobs();
    console.log(`[DEBUG] All jobs:`, jobs);
    res.json(jobs);
    
  } catch (error) {
    console.error('Error getting all jobs:', error);
    res.status(500).json({ error: 'Failed to get jobs' });
  }
});


module.exports = router;
