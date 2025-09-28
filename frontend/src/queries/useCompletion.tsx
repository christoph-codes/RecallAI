import { useState, useCallback, useRef } from "react";
import {
  getCompletion,
  getCompletionStream,
  type CompletionRequest,
} from "@/utils/completion";
import { useUser } from "@/contexts/UserContext";

export type UseCompletionOptions = {
  /** Whether to use streaming responses */
  streaming?: boolean;
  /** Called when each chunk is received (only for streaming) */
  onChunk?: (chunk: string) => void;
  /** Called when the completion starts */
  onStart?: () => void;
  /** Called when the completion completes successfully */
  onComplete?: (fullResponse: string) => void;
  /** Called when an error occurs */
  onError?: (error: Error) => void;
};

type UseCompletionReturn = {
  /** The current completion response (accumulates during streaming) */
  data: string | null;
  /** Whether a completion request is in progress */
  isLoading: boolean;
  /** Any error that occurred during completion */
  error: Error | null;
  /** Whether the last completion was successful */
  isSuccess: boolean;
  /** Whether the completion is currently streaming */
  isStreaming: boolean;
  /** Function to start a completion request */
  complete: (request: CompletionRequest | string) => Promise<void>;
  /** Function to stop streaming completion */
  stop: () => void;
  /** Function to reset the hook state */
  reset: () => void;
};

/**
 * Custom hook for getting AI completions from the RecallAI backend
 *
 * This hook manages the state of completion operations, including both
 * streaming and non-streaming responses. It automatically handles
 * authentication using the user context.
 *
 * @param options Configuration options for the completion hook
 * @returns Completion state and control functions
 *
 * @example
 * ```tsx
 * const { complete, data, isLoading, isStreaming, error } = useCompletion({
 *   streaming: true,
 *   onChunk: (chunk) => console.log('Received:', chunk),
 *   onComplete: (response) => console.log('Complete:', response)
 * });
 *
 * const handleSubmit = async () => {
 *   await complete({
 *     message: "What is the meaning of life?",
 *     configuration: {
 *       temperature: 0.7,
 *       maxTokens: 1000
 *     }
 *   });
 * };
 * ```
 */
export const useCompletion = (
  options: UseCompletionOptions = {}
): UseCompletionReturn => {
  const { user, session } = useUser();
  const [data, setData] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);

  // Use ref for abort controller to persist across re-renders
  const abortControllerRef = useRef<AbortController | null>(null);

  const complete = useCallback(
    async (request: CompletionRequest | string) => {
      // Check if user is authenticated
      if (!user || !session?.access_token) {
        const authError = new Error(
          "User must be authenticated to get completions"
        );
        setError(authError);
        setIsSuccess(false);
        options.onError?.(authError);
        throw authError;
      }

      // Normalize request format
      const completionRequest: CompletionRequest =
        typeof request === "string" ? { message: request } : request;

      // Reset previous state
      setIsLoading(true);
      setError(null);
      setIsSuccess(false);
      setData("");

      // Create abort controller for this request
      abortControllerRef.current = new AbortController();

      try {
        options.onStart?.();

        if (options.streaming) {
          setIsStreaming(true);

          const fullResponse = await getCompletionStream(
            completionRequest,
            session.access_token,
            (chunk: string) => {
              setData((prev) => (prev || "") + chunk);
              options.onChunk?.(chunk);
            }
          );

          setData(fullResponse);
          setIsSuccess(true);
          setError(null);
          options.onComplete?.(fullResponse);
        } else {
          const response = await getCompletion(
            completionRequest,
            session.access_token
          );

          setData(response);
          setIsSuccess(true);
          setError(null);
          options.onComplete?.(response);
        }
      } catch (err) {
        const error =
          err instanceof Error ? err : new Error("Unknown error occurred");
        setError(error);
        setIsSuccess(false);
        setData(null);
        options.onError?.(error);
        throw error;
      } finally {
        setIsLoading(false);
        setIsStreaming(false);
        abortControllerRef.current = null;
      }
    },
    [user, session?.access_token, options]
  );

  const stop = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
      setIsLoading(false);
      setIsStreaming(false);
    }
  }, []);

  const reset = useCallback(() => {
    stop(); // Stop any ongoing request
    setData(null);
    setIsLoading(false);
    setError(null);
    setIsSuccess(false);
    setIsStreaming(false);
  }, [stop]);

  return {
    data,
    isLoading,
    error,
    isSuccess,
    isStreaming,
    complete,
    stop,
    reset,
  };
};

export type {
  CompletionRequest,
  CompletionConfiguration,
} from "@/utils/completion";
