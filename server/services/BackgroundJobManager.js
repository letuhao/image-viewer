const CacheGenerationJob = require('./CacheGenerationJob');
const Logger = require('../utils/logger');

class BackgroundJobManager {
  constructor() {
    this.jobs = new Map();
    this.logger = new Logger('BackgroundJobManager');
  }

  createJob(type, options) {
    const jobId = this.generateJobId();
    
    this.logger.info('Creating job', {
      jobId,
      type,
      options: JSON.stringify(options, null, 2)
    });
    
    let job;
    switch (type) {
      case 'cache-generation':
        job = new CacheGenerationJob(jobId, options);
        break;
      default:
        throw new Error(`Unknown job type: ${type}`);
    }

    this.jobs.set(jobId, job);
    this.logger.debug('Job stored', {
      jobId,
      totalJobs: this.jobs.size
    });
    
    // Start job asynchronously
    job.start().catch(error => {
      this.logger.error('Job failed', {
        jobId,
        error: error.message,
        stack: error.stack
      });
    });

    this.logger.flow('JOB_CREATED_AND_STARTED', {
      jobId,
      type
    });
    return jobId;
  }

  getJob(jobId) {
    return this.jobs.get(jobId);
  }

  cancelJob(jobId) {
    const job = this.jobs.get(jobId);
    if (job) {
      job.cancel();
      this.logger.info('Job cancelled', { jobId });
      return true;
    }
    return false;
  }

  getJobProgress(jobId) {
    const job = this.jobs.get(jobId);
    if (!job) {
      return null;
    }

    return job.getProgress();
  }

  getAllJobs() {
    const jobs = [];
    for (const [jobId, job] of this.jobs) {
      jobs.push({
        jobId,
        type: job.constructor.name,
        progress: job.getProgress()
      });
    }
    return jobs;
  }

  cleanupCompletedJobs() {
    this.logger.debug('Cleanup check', { totalJobs: this.jobs.size });
    
    for (const [jobId, job] of this.jobs) {
      this.logger.debug('Job status check', {
        jobId,
        running: job.isRunning,
        status: job.progress.status
      });
      
      if (!job.isRunning && 
          (job.progress.status === 'completed' || 
           job.progress.status === 'failed' || 
           job.progress.status === 'cancelled')) {
        
        // Keep completed jobs for 2 hours, then remove (increased from 1 hour)
        const completedTime = job.progress.completedAt || new Date();
        const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000);
        
        if (completedTime < twoHoursAgo) {
          this.jobs.delete(jobId);
          this.logger.info('Cleaned up completed job', {
            jobId,
            completedAt: completedTime
          });
        } else {
          this.logger.debug('Keeping completed job', {
            jobId,
            completedAt: completedTime
          });
        }
      }
    }
    
    this.logger.debug('After cleanup', { totalJobs: this.jobs.size });
  }

  generateJobId() {
    return `job_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

// Singleton instance
const jobManager = new BackgroundJobManager();

// Cleanup completed jobs every 2 hours (less frequent)
setInterval(() => {
  jobManager.cleanupCompletedJobs();
}, 2 * 60 * 60 * 1000);

module.exports = jobManager;
