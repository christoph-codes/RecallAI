"use client";

import { useCallback, useState } from "react";
import { useUser } from "@/contexts/UserContext";
import { deleteMemory } from "@/utils/memory";

type UseDeleteMemoryReturn = {
  isLoading: boolean;
  error: Error | null;
  deleteMemoryById: (id: string) => Promise<void>;
};

export const useDeleteMemory = (): UseDeleteMemoryReturn => {
  const { user, session } = useUser();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const deleteMemoryById = useCallback(
    async (id: string) => {
      if (!user || !session?.access_token) {
        const authError = new Error(
          "User must be authenticated to delete memories"
        );
        setError(authError);
        throw authError;
      }

      setIsLoading(true);
      setError(null);

      try {
        await deleteMemory(session.access_token, id);
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("Failed to delete memory");
        setError(error);
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [user, session?.access_token]
  );

  return {
    isLoading,
    error,
    deleteMemoryById,
  };
};
