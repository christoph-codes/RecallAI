import { useState, useEffect, useCallback } from 'react';
import { healthCheck, type HealthCheckResponse } from '@/utils/healthCheck';

interface UseHealthCheckOptions {
  /** Whether to run the health check automatically on mount */
  autoCheck?: boolean;
  /** Interval in milliseconds to automatically re-check health (0 = no interval) */
  interval?: number;
}

interface UseHealthCheckReturn {
  /** The health check data */
  data: HealthCheckResponse | null;
  /** Whether a health check request is in progress */
  isLoading: boolean;
  /** Any error that occurred during the health check */
  error: Error | null;
  /** Whether the backend is healthy (convenience boolean) */
  isHealthy: boolean;
  /** Function to manually trigger a health check */
  refetch: () => Promise<void>;
}

/**
 * Custom hook for managing backend health check state
 * 
 * @param options Configuration options for the health check
 * @returns Health check state and control functions
 */
export const useHealthCheck = ({
  autoCheck = true,
  interval = 0
}: UseHealthCheckOptions = {}): UseHealthCheckReturn => {
  const [data, setData] = useState<HealthCheckResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const executeHealthCheck = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await healthCheck();
      setData(result);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Unknown health check error');
      setError(error);
      console.error('Health check failed:', error.message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Auto-check on mount if enabled
  useEffect(() => {
    if (autoCheck) {
      executeHealthCheck();
    }
  }, [autoCheck, executeHealthCheck]);

  // Set up interval checking if specified
  useEffect(() => {
    if (interval > 0) {
      const intervalId = setInterval(executeHealthCheck, interval);
      return () => clearInterval(intervalId);
    }
  }, [interval, executeHealthCheck]);

  const isHealthy = data?.status === 'healthy';

  return {
    data,
    isLoading,
    error,
    isHealthy,
    refetch: executeHealthCheck
  };
};