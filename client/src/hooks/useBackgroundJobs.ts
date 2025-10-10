import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-hot-toast';
import {
  getBackgroundJobs,
  getBackgroundJobById,
  getJobStatistics,
  cancelJob,
  cleanupJobs,
  type BackgroundJobsResponse,
  type BackgroundJob,
  type JobStatistics,
} from '../services/backgroundJobsApi';

/**
 * Hook to fetch background jobs with pagination and filtering
 */
export const useBackgroundJobs = (
  page: number = 1,
  limit: number = 20,
  status?: string,
  jobType?: string
) => {
  return useQuery<BackgroundJobsResponse, Error>({
    queryKey: ['backgroundJobs', page, limit, status, jobType],
    queryFn: () => getBackgroundJobs(page, limit, status, jobType),
    staleTime: 5 * 1000, // 5 seconds (jobs update frequently)
    refetchInterval: 10 * 1000, // Auto-refetch every 10 seconds for live updates
  });
};

/**
 * Hook to fetch a single job by ID
 */
export const useBackgroundJob = (id: string | null) => {
  return useQuery<BackgroundJob, Error>({
    queryKey: ['backgroundJob', id],
    queryFn: () => getBackgroundJobById(id!),
    enabled: !!id,
    staleTime: 2 * 1000, // 2 seconds
    refetchInterval: 5 * 1000, // Auto-refetch every 5 seconds for live updates
  });
};

/**
 * Hook to fetch job statistics
 */
export const useJobStatistics = () => {
  return useQuery<JobStatistics, Error>({
    queryKey: ['jobStatistics'],
    queryFn: getJobStatistics,
    staleTime: 10 * 1000, // 10 seconds
    refetchInterval: 15 * 1000, // Auto-refetch every 15 seconds
  });
};

/**
 * Hook to cancel a job
 */
export const useCancelJob = () => {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: cancelJob,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['backgroundJobs'] });
      queryClient.invalidateQueries({ queryKey: ['jobStatistics'] });
      toast.success('Job cancelled successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to cancel job: ${error.message}`);
    },
  });
};

/**
 * Hook to cleanup old jobs
 */
export const useCleanupJobs = () => {
  const queryClient = useQueryClient();

  return useMutation<{ deletedCount: number }, Error, number>({
    mutationFn: cleanupJobs,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['backgroundJobs'] });
      queryClient.invalidateQueries({ queryKey: ['jobStatistics'] });
      toast.success(`Cleaned up ${data.deletedCount} old jobs!`);
    },
    onError: (error) => {
      toast.error(`Failed to cleanup jobs: ${error.message}`);
    },
  });
};

