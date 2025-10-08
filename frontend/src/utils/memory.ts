/**
 * Memory API types and utilities for the RecallAI backend
 */

export type CreateMemoryRequest = {
  title?: string;
  content: string;
  contentType?: "text" | "document" | "note" | "conversation";
  metadata?: Record<string, unknown>;
};

export type MemoryResponse = {
  id: string;
  title: string | null;
  content: string;
  contentType: string;
  metadata: Record<string, unknown> | null;
  createdAt: string;
  updatedAt: string;
};

export type MemoryListResponse = {
  memories: MemoryResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type MemorySearchResult = {
  id: string;
  title: string | null;
  content: string;
  contentType: string;
  similarityScore: number;
  combinedScore: number;
  searchMethod: string;
  createdAt: string;
  metadata: Record<string, unknown> | null;
};

export type MemorySearchResponse = {
  results: MemorySearchResult[];
  query: string;
  resultCount: number;
  executionTimeMs: number;
  hydeUsed: boolean;
  hypotheticalDocument?: string | null;
};

const API_CONFIG = {
  BASE_URL: process.env.NEXT_PUBLIC_API_URL,
  ENDPOINTS: {
    MEMORIES: "/api/memory",
    SEARCH: "/api/memory/search",
  },
  TIMEOUT: 30000,
} as const;

const extractErrorMessage = async (response: Response) => {
  let message = `HTTP ${response.status}: ${response.statusText}`;
  try {
    const body = await response.json();
    message =
      body.message ??
      body.Message ??
      body.error ??
      body.Error ??
      message;
  } catch {
    // ignore JSON parse errors
  }

  return message;
};

const createAbort = (timeoutMs = API_CONFIG.TIMEOUT) => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  return { controller, timeoutId };
};

export const createMemory = async (
  request: CreateMemoryRequest,
  accessToken: string
): Promise<MemoryResponse> => {
  const { controller, timeoutId } = createAbort();

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
      throw new Error(await extractErrorMessage(response));
    }

    return (await response.json()) as MemoryResponse;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Memory creation request timed out");
    }
    throw error instanceof Error
      ? error
      : new Error("An unexpected error occurred while creating memory");
  }
};

export const listMemories = async (
  accessToken: string,
  page = 1,
  pageSize = 20
): Promise<MemoryListResponse> => {
  const { controller, timeoutId } = createAbort();

  try {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.MEMORIES}?${params.toString()}`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(await extractErrorMessage(response));
    }

    return (await response.json()) as MemoryListResponse;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Memory list request timed out");
    }
    throw error instanceof Error
      ? error
      : new Error("An unexpected error occurred while fetching memories");
  }
};

export const getMemory = async (
  accessToken: string,
  id: string
): Promise<MemoryResponse> => {
  const { controller, timeoutId } = createAbort();

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.MEMORIES}/${id}`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(await extractErrorMessage(response));
    }

    return (await response.json()) as MemoryResponse;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Memory fetch request timed out");
    }
    throw error instanceof Error
      ? error
      : new Error("An unexpected error occurred while fetching the memory");
  }
};

export const deleteMemory = async (
  accessToken: string,
  id: string
): Promise<void> => {
  const { controller, timeoutId } = createAbort();

  try {
    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.MEMORIES}/${id}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(await extractErrorMessage(response));
    }
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Memory deletion request timed out");
    }
    throw error instanceof Error
      ? error
      : new Error("An unexpected error occurred while deleting the memory");
  }
};

export type MemorySearchOptions = {
  query: string;
  limit?: number;
  threshold?: number;
  useHyde?: boolean;
};

export const searchMemories = async (
  accessToken: string,
  { query, limit = 10, threshold = 0.7, useHyde = false }: MemorySearchOptions
): Promise<MemorySearchResponse> => {
  const { controller, timeoutId } = createAbort();

  try {
    const params = new URLSearchParams({
      query,
      limit: limit.toString(),
      threshold: threshold.toString(),
      useHyde: useHyde ? "true" : "false",
    });

    const response = await fetch(
      `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SEARCH}?${params.toString()}`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
        signal: controller.signal,
      }
    );

    clearTimeout(timeoutId);

    if (!response.ok) {
      throw new Error(await extractErrorMessage(response));
    }

    return (await response.json()) as MemorySearchResponse;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Memory search request timed out");
    }
    throw error instanceof Error
      ? error
      : new Error("An unexpected error occurred while searching memories");
  }
};
