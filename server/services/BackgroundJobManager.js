const CacheGenerationJob = require('./CacheGenerationJob');

class BackgroundJobManager {
  constructor() {
    this.jobs = new Map();
  }

  createJob(type, options) {
    const jobId = this.generateJobId();
    
    console.log(`[JOB-MANAGER] Creating job ${jobId} of type ${type}`);
    console.log(`[JOB-MANAGER] Options:`, JSON.stringify(options, null, 2));
    
    let job;
    switch (type) {
      case 'cache-generation':
        job = new CacheGenerationJob(jobId, options);
        break;
      default:
        throw new Error(`Unknown job type: ${type}`);
    }

    this.jobs.set(jobId, job);
    console.log(`[JOB-MANAGER] Job ${jobId} stored. Total jobs: ${this.jobs.size}`);
    
    // Start job asynchronously
    job.start().catch(error => {
      console.error(`[JOB-MANAGER] Job ${jobId} failed:`, error);
    });

    console.log(`[JOB-MANAGER] ✅ Created and started job ${jobId} of type ${type}`);
    return jobId;
  }

  getJob(jobId) {
    return this.jobs.get(jobId);
  }

  cancelJob(jobId) {
    const job = this.jobs.get(jobId);
    if (job) {
      job.cancel();
      console.log(`[JOB-MANAGER] Cancelled job ${jobId}`);
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
    console.log(`[JOB-MANAGER] Cleanup check - Total jobs: ${this.jobs.size}`);
    
    for (const [jobId, job] of this.jobs) {
      console.log(`[JOB-MANAGER] Job ${jobId}: running=${job.isRunning}, status=${job.progress.status}`);
      
      if (!job.isRunning && 
          (job.progress.status === 'completed' || 
           job.progress.status === 'failed' || 
           job.progress.status === 'cancelled')) {
        
        // Keep completed jobs for 2 hours, then remove (increased from 1 hour)
        const completedTime = job.progress.completedAt || new Date();
        const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000);
        
        if (completedTime < twoHoursAgo) {
          this.jobs.delete(jobId);
          console.log(`[JOB-MANAGER] Cleaned up completed job ${jobId}`);
        } else {
          console.log(`[JOB-MANAGER] Keeping completed job ${jobId} (completed at ${completedTime})`);
        }
      }
    }
    
    console.log(`[JOB-MANAGER] After cleanup - Total jobs: ${this.jobs.size}`);
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
