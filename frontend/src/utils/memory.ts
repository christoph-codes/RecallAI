/**
 * Memory API types and utilities for the RecallAI backend
 */

/**
 * Request payload for creating a new memory
 */
export type CreateMemoryRequest = {
  title?: string;
  content: string;
  contentType?: "text" | "document" | "note" | "conversation";
  metadata?: Record<string, unknown>;
};

/**
 * Response from creating a memory
 */
export type MemoryResponse = {
  id: string;
  title: string | null;
  content: string;
  contentType: string;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
};

/**
 * Configuration for the Memory API client
 */
const API_CONFIG = {
  BASE_URL: process.env.NEXT_PUBLIC_API_URL,
  ENDPOINTS: {
    MEMORIES: "/api/memory",
  },
  TIMEOUT: 30000, // 30 seconds for memory operations
} as const;

/**
 * Creates a new memory in the backend
 * @param request - The memory data to create
 * @param accessToken - JWT access token for authentication
 * @returns Promise<MemoryResponse> - The created memory
 * @throws Error if the request fails or times out
 */
export const createMemory = async (
  request: CreateMemoryRequest,
  accessToken: string
): Promise<MemoryResponse> => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT);

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.MEMORIES}`,
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
        if (errorData.message) {
          errorMessage = errorData.message;
        } else if (errorData.Message) {
          errorMessage = errorData.Message;
        }
      } catch {
        // If we can't parse the error response, use the default message
      }

      throw new Error(errorMessage);
    }

    const data: MemoryResponse = await response.json();
    return data;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof Error) {
      if (error.name === "AbortError") {
        throw new Error("Memory creation request timed out");
      }
      throw error;
    }

    throw new Error("An unexpected error occurred while creating memory");
  }
};
