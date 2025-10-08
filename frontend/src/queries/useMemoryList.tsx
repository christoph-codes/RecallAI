"use client";

import { useCallback, useEffect, useState } from "react";
import { useUser } from "@/contexts/UserContext";
import { listMemories, type MemoryResponse } from "@/utils/memory";

type UseMemoryListOptions = {
  pageSize?: number;
  autoFetch?: boolean;
};

type UseMemoryListReturn = {
  memories: MemoryResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  isLoading: boolean;
  error: Error | null;
  goToPage: (targetPage: number) => Promise<void>;
  refetch: () => Promise<void>;
};

export const useMemoryList = (
  { pageSize = 10, autoFetch = true }: UseMemoryListOptions = {}
): UseMemoryListReturn => {
  const { user, session } = useUser();
  const [memories, setMemories] = useState<MemoryResponse[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const fetchPage = useCallback(
    async (targetPage: number) => {
      if (!user || !session?.access_token) {
        const authError = new Error(
          "User must be authenticated to load memories"
        );
        setError(authError);
        throw authError;
      }

      setIsLoading(true);
      setError(null);

      try {
        const response = await listMemories(
          session.access_token,
          targetPage,
          pageSize
        );
        setMemories(response.memories);
        setTotalCount(response.totalCount);
        setPage(response.page);
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("Failed to load memories");
        setError(error);
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [user, session?.access_token, pageSize]
  );

  useEffect(() => {
    if (autoFetch) {
      fetchPage(1).catch(() => {
        // error state already set
      });
    }
  }, [autoFetch, fetchPage]);

  const goToPage = useCallback(
    async (targetPage: number) => {
      await fetchPage(targetPage);
    },
    [fetchPage]
  );

  const refetch = useCallback(async () => {
    await fetchPage(page);
  }, [fetchPage, page]);

  return {
    memories,
    page,
    pageSize,
    totalCount,
    isLoading,
    error,
    goToPage,
    refetch,
  };
};

export type { MemoryResponse } from "@/utils/memory";
