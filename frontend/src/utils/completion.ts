/**
 * Completion API types and utilities for the RecallAI backend
 */

/**
 * Configuration options for completion requests
 */
export type CompletionConfiguration = {
  model?: string;
  temperature?: number;
  maxTokens?: number;
  enableMemorySearch?: boolean;
  maxMemoryResults?: number;
  memoryThreshold?: number;
};

/**
 * Request payload for completion
 */
export type CompletionRequest = {
  message: string;
  configuration?: CompletionConfiguration;
};

/**
 * Response from completion endpoint
 */
export type CompletionResponse = {
  content: string;
  isComplete: boolean;
  memoryUsed?: boolean;
  memoryResults?: number;
};

/**
 * Configuration for the Completion API client
 */
const API_CONFIG = {
  BASE_URL: process.env.NEXT_PUBLIC_API_URL,
  ENDPOINTS: {
    COMPLETION: "/api/completion",
    COMPLETION_SSE: "/api/completion/sse",
  },
  TIMEOUT: 60000, // 60 seconds for completion operations
} as const;

/**
 * Get completion from the backend (non-streaming)
 * @param request - The completion request
 * @param accessToken - JWT access token for authentication
 * @returns Promise<string> - The complete response text
 * @throws Error if the request fails or times out
 */
export const getCompletion = async (
  request: CompletionRequest,
  accessToken: string
): Promise<string> => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT);

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.COMPLETION}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(request),
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      // Try to get error details from the response
      let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
      try {
        const errorData = await response.json();
        if (errorData.error) {
          errorMessage = errorData.error;
        }
      } catch {
        // If we can't parse the error response, use the default message
      }

      throw new Error(errorMessage);
    }

    // For streaming response, read the entire stream
    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error("No response body");
    }

    let fullResponse = "";
    const decoder = new TextDecoder();

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });
        fullResponse += chunk;
      }
    } finally {
      reader.releaseLock();
    }

    return fullResponse;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof Error) {
      if (error.name === "AbortError") {
        throw new Error("Completion request timed out");
      }
      throw error;
    }

    throw new Error("An unexpected error occurred while getting completion");
  }
};

/**
 * Get streaming completion from the backend
 * @param request - The completion request
 * @param accessToken - JWT access token for authentication
 * @param onChunk - Callback for each chunk of the response
 * @returns Promise<string> - The complete response text
 * @throws Error if the request fails or times out
 */
export const getCompletionStream = async (
  request: CompletionRequest,
  accessToken: string,
  onChunk: (chunk: string) => void
): Promise<string> => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT);

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.COMPLETION}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(request),
        signal: controller.signal,
      }
    );
    console.log("response", response);

    clearTimeout(timeoutId);

    if (!response.ok) {
      let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
      try {
        const errorData = await response.json();
        if (errorData.error) {
          errorMessage = errorData.error;
        }
      } catch {
        // If we can't parse the error response, use the default message
      }

      throw new Error(errorMessage);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error("No response body");
    }

    let fullResponse = "";
    const decoder = new TextDecoder();

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });
        fullResponse += chunk;
        onChunk(chunk);
      }
    } finally {
      reader.releaseLock();
    }

    return fullResponse;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof Error) {
      if (error.name === "AbortError") {
        throw new Error("Completion request timed out");
      }
      throw error;
    }

    throw new Error("An unexpected error occurred while getting completion");
  }
};
