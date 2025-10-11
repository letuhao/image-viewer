import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:11001/api/v1';

export interface ScheduledJob {
  id: string;
  name: string;
  description?: string;
  jobType: string;
  scheduleType: string;
  cronExpression?: string;
  intervalMinutes?: number;
  isEnabled: boolean;
  parameters: Record<string, any>;
  lastRunAt?: string;
  nextRunAt?: string;
  lastRunDuration?: number;
  lastRunStatus?: string;
  runCount: number;
  successCount: number;
  failureCount: number;
  lastErrorMessage?: string;
  priority: number;
  timeoutMinutes: number;
  maxRetryAttempts: number;
  createdAt: string;
  updatedAt: string;
}

export interface ScheduledJobRun {
  id: string;
  scheduledJobId: string;
  scheduledJobName: string;
  jobType: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  duration?: number;
  errorMessage?: string;
  result?: Record<string, any>;
  triggeredBy: string;
}

export interface JobRunsResponse {
  data: ScheduledJobRun[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const schedulerApi = {
  // Get all scheduled jobs
  getAllJobs: async (): Promise<ScheduledJob[]> => {
    const response = await axios.get(`${API_BASE_URL}/scheduledjobs`);
    return response.data;
  },

  // Get job by ID
  getJob: async (id: string): Promise<ScheduledJob> => {
    const response = await axios.get(`${API_BASE_URL}/scheduledjobs/${id}`);
    return response.data;
  },

  // Get job by library ID
  getJobByLibrary: async (libraryId: string): Promise<ScheduledJob | null> => {
    try {
      const response = await axios.get(`${API_BASE_URL}/scheduledjobs/library/${libraryId}`);
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  // Enable job
  enableJob: async (id: string): Promise<void> => {
    await axios.post(`${API_BASE_URL}/scheduledjobs/${id}/enable`);
  },

  // Disable job
  disableJob: async (id: string): Promise<void> => {
    await axios.post(`${API_BASE_URL}/scheduledjobs/${id}/disable`);
  },

  // Delete job
  deleteJob: async (id: string): Promise<void> => {
    await axios.delete(`${API_BASE_URL}/scheduledjobs/${id}`);
  },

  // Get job execution history
  getJobRuns: async (id: string, page: number = 1, pageSize: number = 20): Promise<JobRunsResponse> => {
    const response = await axios.get(`${API_BASE_URL}/scheduledjobs/${id}/runs`, {
      params: { page, pageSize }
    });
    return response.data;
  },

  // Get active jobs
  getActiveJobs: async (): Promise<ScheduledJob[]> => {
    const response = await axios.get(`${API_BASE_URL}/scheduledjobs/active`);
    return response.data;
  },

  // Get recent job runs
  getRecentRuns: async (limit: number = 50): Promise<ScheduledJobRun[]> => {
    const response = await axios.get(`${API_BASE_URL}/scheduledjobs/runs/recent`, {
      params: { limit }
    });
    return response.data;
  },
};

