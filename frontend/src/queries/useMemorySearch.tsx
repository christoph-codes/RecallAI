"use client";

import { useCallback, useState } from "react";
import { useUser } from "@/contexts/UserContext";
import {
  searchMemories,
  type MemorySearchOptions,
  type MemorySearchResponse,
} from "@/utils/memory";

type UseMemorySearchReturn = {
  data: MemorySearchResponse | null;
  isLoading: boolean;
  error: Error | null;
  search: (options: MemorySearchOptions) => Promise<MemorySearchResponse>;
  reset: () => void;
};

export const useMemorySearch = (): UseMemorySearchReturn => {
  const { user, session } = useUser();
  const [data, setData] = useState<MemorySearchResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const search = useCallback(
    async (options: MemorySearchOptions) => {
      if (!user || !session?.access_token) {
        const authError = new Error(
          "User must be authenticated to search memories"
        );
        setError(authError);
        throw authError;
      }

      setIsLoading(true);
      setError(null);

      try {
        const response = await searchMemories(session.access_token, options);
        setData(response);
        return response;
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("Failed to search memories");
        setError(error);
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [user, session?.access_token]
  );

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setIsLoading(false);
  }, []);

  return {
    data,
    isLoading,
    error,
    search,
    reset,
  };
};

export type {
  MemorySearchOptions,
  MemorySearchResponse,
  MemorySearchResult,
} from "@/utils/memory";
