import { useState, useCallback } from "react";
import {
  createMemory,
  type CreateMemoryRequest,
  type MemoryResponse,
} from "@/utils/memory";
import { useUser } from "@/contexts/UserContext";

interface UseCreateMemoryReturn {
  /** The created memory data (null until a memory is created) */
  data: MemoryResponse | null;
  /** Whether a memory creation request is in progress */
  isLoading: boolean;
  /** Any error that occurred during memory creation */
  error: Error | null;
  /** Whether the last creation was successful */
  isSuccess: boolean;
  /** Function to create a new memory */
  createMemoryMutation: (request: CreateMemoryRequest) => Promise<void>;
  /** Function to reset the hook state */
  reset: () => void;
}

/**
 * Custom hook for creating memories in the RecallAI backend
 *
 * This hook manages the state of memory creation operations, including
 * loading states, error handling, and success states. It automatically
 * handles authentication using the user context.
 *
 * @returns Memory creation state and control functions
 *
 * @example
 * ```tsx
 * const { createMemoryMutation, isLoading, error, isSuccess } = useCreateMemory();
 *
 * const handleSubmit = async (content: string) => {
 *   try {
 *     await createMemoryMutation({
 *       content,
 *       title: "My Memory",
 *       contentType: "text"
 *     });
 *   } catch (err) {
 *     console.error('Failed to create memory:', err);
 *   }
 * };
 * ```
 */
export const useCreateMemory = (): UseCreateMemoryReturn => {
  const { user, session } = useUser();
  const [data, setData] = useState<MemoryResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const createMemoryMutation = useCallback(
    async (request: CreateMemoryRequest) => {
      // Check if user is authenticated
      if (!user || !session?.access_token) {
        const authError = new Error(
          "User must be authenticated to create memories"
        );
        setError(authError);
        setIsSuccess(false);
        throw authError;
      }

      // Reset previous state
      setIsLoading(true);
      setError(null);
      setIsSuccess(false);
      setData(null);

      try {
        const result = await createMemory(request, session.access_token);

        setData(result);
        setIsSuccess(true);
        setError(null);
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("Unknown error occurred");
        setError(error);
        setIsSuccess(false);
        setData(null);
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [user, session?.access_token]
  );

  const reset = useCallback(() => {
    setData(null);
    setIsLoading(false);
    setError(null);
    setIsSuccess(false);
  }, []);

  return {
    data,
    isLoading,
    error,
    isSuccess,
    createMemoryMutation,
    reset,
  };
};

export type { CreateMemoryRequest, MemoryResponse } from "@/utils/memory";
