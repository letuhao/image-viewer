import api from './api';

// ============================================================================
// Types
// ============================================================================

export interface BackgroundJob {
  id: string;
  jobType: string;
  status: string;
  progress: number;
  totalItems: number;
  completedItems: number;
  currentItem?: string;
  message?: string;
  errorMessage?: string;
  errors?: string[];
  parameters?: string;
  result?: string;
  startedAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
  duration?: number;
  stages?: Record<string, JobStageInfo>;
  currentStage?: string;
  collectionId?: string;
}

export interface JobStageInfo {
  stageName: string;
  status: string;
  progress: number;
  totalItems: number;
  completedItems: number;
  message?: string;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface JobStatistics {
  total: number;
  pending: number;
  running: number;
  completed: number;
  failed: number;
  cancelled: number;
  avgDuration: number;
  byType: Array<{
    jobType: string;
    count: number;
    running: number;
    completed: number;
    failed: number;
  }>;
}

export interface BackgroundJobsResponse {
  data: BackgroundJob[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
}

// ============================================================================
// Background Jobs API
// ============================================================================

/**
 * Get all background jobs with pagination and filtering
 */
export const getBackgroundJobs = async (
  page: number = 1,
  limit: number = 20,
  status?: string,
  jobType?: string
): Promise<BackgroundJobsResponse> => {
  const params = new URLSearchParams();
  params.append('page', page.toString());
  params.append('limit', limit.toString());
  if (status) params.append('status', status);
  if (jobType) params.append('jobType', jobType);

  const response = await api.get<BackgroundJobsResponse>(`/backgroundjobs?${params.toString()}`);
  return response.data;
};

/**
 * Get job by ID
 */
export const getBackgroundJobById = async (id: string): Promise<BackgroundJob> => {
  const response = await api.get<BackgroundJob>(`/backgroundjobs/${id}`);
  return response.data;
};

/**
 * Get job statistics
 */
export const getJobStatistics = async (): Promise<JobStatistics> => {
  const response = await api.get<JobStatistics>('/backgroundjobs/statistics');
  return response.data;
};

/**
 * Cancel a job
 */
export const cancelJob = async (id: string): Promise<void> => {
  await api.post(`/backgroundjobs/${id}/cancel`);
};

/**
 * Cleanup old jobs
 */
export const cleanupJobs = async (olderThanDays: number = 7): Promise<{ deletedCount: number }> => {
  const response = await api.delete<{ deletedCount: number }>(`/backgroundjobs/cleanup?olderThanDays=${olderThanDays}`);
  return response.data;
};

