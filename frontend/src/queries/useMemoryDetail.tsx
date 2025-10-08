"use client";

import { useCallback, useEffect, useState } from "react";
import { useUser } from "@/contexts/UserContext";
import { getMemory, type MemoryResponse } from "@/utils/memory";

type UseMemoryDetailOptions = {
  enabled?: boolean;
};

type UseMemoryDetailReturn = {
  memory: MemoryResponse | null;
  isLoading: boolean;
  error: Error | null;
  refetch: () => Promise<void>;
};

export const useMemoryDetail = (
  memoryId: string | null,
  { enabled = true }: UseMemoryDetailOptions = {}
): UseMemoryDetailReturn => {
  const { user, session } = useUser();
  const [memory, setMemory] = useState<MemoryResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const fetchDetail = useCallback(async () => {
    if (!enabled || !memoryId) {
      setMemory(null);
      return;
    }

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
      const result = await getMemory(session.access_token, memoryId);
      setMemory(result);
    } catch (err) {
      const error =
        err instanceof Error ? err : new Error("Failed to load memory");
      setError(error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  }, [enabled, memoryId, session?.access_token, user]);

  useEffect(() => {
    fetchDetail().catch(() => {
      // error state already set
    });
  }, [fetchDetail]);

  return {
    memory,
    isLoading,
    error,
    refetch: fetchDetail,
  };
};

export type { MemoryResponse } from "@/utils/memory";
