const EventEmitter = require('events');

class BackgroundJobManager extends EventEmitter {
  constructor() {
    super();
    this.jobs = new Map();
    this.jobCounter = 0;
  }

  createJob(type, data) {
    const jobId = `job_${++this.jobCounter}_${Date.now()}`;
    const job = {
      id: jobId,
      type,
      data,
      status: 'pending', // pending, running, completed, failed, cancelled
      progress: 0,
      total: 0,
      current: 0,
      results: null,
      error: null,
      createdAt: new Date(),
      startedAt: null,
      completedAt: null
    };

    this.jobs.set(jobId, job);
    this.emit('jobCreated', job);
    return jobId;
  }

  startJob(jobId, workerFunction) {
    const job = this.jobs.get(jobId);
    if (!job) {
      throw new Error(`Job ${jobId} not found`);
    }

    if (job.status !== 'pending') {
      throw new Error(`Job ${jobId} is not in pending state`);
    }

    job.status = 'running';
    job.startedAt = new Date();
    this.emit('jobStarted', job);

    // Create updateProgress callback
    const updateProgress = (current, total, message = null) => {
      job.progress = total > 0 ? Math.round((current / total) * 100) : 0;
      job.current = current;
      job.total = total;
      if (message) {
        job.message = message;
      }
      this.emit('jobProgress', job);
    };

    // Run the job in the background
    workerFunction(updateProgress)
      .then((results) => {
        job.status = 'completed';
        job.progress = 100;
        job.results = results;
        job.completedAt = new Date();
        this.emit('jobCompleted', job);
      })
      .catch((error) => {
        job.status = 'failed';
        job.error = error.message;
        job.completedAt = new Date();
        this.emit('jobFailed', job);
      });

    return job;
  }

  updateJobProgress(jobId, progress, current, total, message = null) {
    const job = this.jobs.get(jobId);
    if (!job) return;

    job.progress = Math.min(100, Math.max(0, progress));
    job.current = current;
    job.total = total;
    if (message) {
      job.message = message;
    }

    this.emit('jobProgress', job);
  }

  getJob(jobId) {
    return this.jobs.get(jobId);
  }

  getAllJobs() {
    return Array.from(this.jobs.values());
  }

  getJobsByStatus(status) {
    return Array.from(this.jobs.values()).filter(job => job.status === status);
  }

  hasRunningJob(type = null) {
    const runningJobs = Array.from(this.jobs.values()).filter(job => job.status === 'running');
    if (type) {
      return runningJobs.some(job => job.type === type);
    }
    return runningJobs.length > 0;
  }

  isJobCancelled(jobId) {
    const job = this.jobs.get(jobId);
    return job ? job.status === 'cancelled' : true;
  }

  cancelJob(jobId) {
    const job = this.jobs.get(jobId);
    if (!job) return false;

    if (job.status === 'pending' || job.status === 'running') {
      job.status = 'cancelled';
      job.completedAt = new Date();
      this.emit('jobCancelled', job);
      return true;
    }

    return false;
  }

  cleanupOldJobs(maxAge = 24 * 60 * 60 * 1000) { // 24 hours
    const cutoff = new Date(Date.now() - maxAge);
    const jobsToDelete = [];

    for (const [jobId, job] of this.jobs) {
      if (job.createdAt < cutoff && (job.status === 'completed' || job.status === 'failed' || job.status === 'cancelled')) {
        jobsToDelete.push(jobId);
      }
    }

    jobsToDelete.forEach(jobId => {
      this.jobs.delete(jobId);
    });

    if (jobsToDelete.length > 0) {
      this.emit('jobsCleaned', jobsToDelete.length);
    }
  }
}

// Create a singleton instance
const jobManager = new BackgroundJobManager();

// Cleanup old jobs every hour
setInterval(() => {
  jobManager.cleanupOldJobs();
}, 60 * 60 * 1000);

module.exports = jobManager;
