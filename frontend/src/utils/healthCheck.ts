/**
 * Health check response from the backend API
 */
export type HealthCheckResponse = {
  status: "healthy" | "unhealthy";
  timestamp: string;
  database: "connected" | "disconnected";
  version?: string;
  error?: string;
};

/**
 * Configuration for the API client
 */
const API_CONFIG = {
  BASE_URL: process.env.NEXT_PUBLIC_API_URL,
  ENDPOINTS: {
    HEALTH: "/api/health",
  },
  TIMEOUT: 10000, // 10 seconds
} as const;

/**
 * Performs a health check against the backend API
 * @returns Promise<HealthCheckResponse> - Health check result
 * @throws Error if the request fails or times out
 */
export const healthCheck = async (): Promise<HealthCheckResponse> => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT);

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.HEALTH}`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(
        `Health check failed: ${response.status} ${response.statusText}. ${
          errorData.error || ""
        }`
      );
    }

    const healthData: HealthCheckResponse = await response.json();
    return healthData;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof Error) {
      if (error.name === "AbortError") {
        throw new Error("Health check request timed out");
      }
      throw error;
    }

    throw new Error("Health check failed with unknown error");
  }
};

/**
 * Simple health check that returns a boolean result
 * @returns Promise<boolean> - true if healthy, false if unhealthy
 */
export const isBackendHealthy = async (): Promise<boolean> => {
  try {
    const result = await healthCheck();
    return result.status === "healthy";
  } catch (error) {
    console.error("Health check failed:", error);
    return false;
  }
};
