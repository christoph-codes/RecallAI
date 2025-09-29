"use client";

import Button from "@/components/button";
import LoadingSpinner from "@/components/LoadingSpinner";
import { useAuthGuard } from "@/hooks/useAuthGuard";
import { useCreateMemory, useCompletion } from "@/queries";
import { useUser } from "@/contexts/UserContext";
import { FormEvent, useState, useRef, useEffect } from "react";

type Response = {
  id: string;
  query: string;
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
  memoryCreated?: boolean;
};

// Utility function to format timestamps
const formatTime = (date: Date) => {
  return new Intl.DateTimeFormat("en-US", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);
};

const Dashboard = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const { session, refreshUser } = useUser();
  const [responses, setResponses] = useState<Response[]>([]);
  const [inputMessage, setInputMessage] = useState("");
  const responsesEndRef = useRef<HTMLDivElement>(null);

  const { createMemoryMutation: createMemory } = useCreateMemory();
  const {
    complete,
    data: completionData,
    isLoading: isCompleting,
    reset: resetCompletion,
  } = useCompletion({
    streaming: true,
    onChunk: () => {
      // Update the streaming response with new chunk
      setResponses((prev) => {
        const newResponses = [...prev];
        const lastResponse = newResponses[newResponses.length - 1];
        if (lastResponse?.isStreaming) {
          lastResponse.content = completionData || "";
        }
        return newResponses;
      });
    },
    onComplete: (fullResponse) => {
      // Mark the response as complete
      setResponses((prev) => {
        const newResponses = [...prev];
        const lastResponse = newResponses[newResponses.length - 1];
        if (lastResponse?.isStreaming) {
          lastResponse.isStreaming = false;
          lastResponse.content = fullResponse;
        }
        return newResponses;
      });
    },
  });

  // Auto-scroll to bottom when responses change
  useEffect(() => {
    responsesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [responses, completionData]);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <LoadingSpinner
          size="lg"
          message="Setting up your workspace..."
          variant="branded"
        />
      </div>
    );
  }

  const handleSendMessage = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!inputMessage.trim() || isCompleting) return;

    const query = inputMessage.trim();

    // Create response placeholder for streaming
    const newResponse: Response = {
      id: Date.now().toString(),
      query: query,
      content: "",
      timestamp: new Date(),
      isStreaming: true,
    };

    setResponses((prev) => [...prev, newResponse]);

    // Clear input
    setInputMessage("");

    try {
      // Build conversation context from previous responses
      const conversationHistory = responses
        .map(
          (response) =>
            `Query: ${response.query}\nResponse: ${response.content}`
        )
        .join("\n\n");

      const promptWithContext = conversationHistory
        ? `Previous interactions:\n${conversationHistory}\n\nCurrent query: ${query}\n\nResponse:`
        : query;

      await complete({
        message: promptWithContext,
        configuration: {
          temperature: 0.7,
          maxTokens: 500,
          enableMemorySearch: true,
        },
      });

      // Optionally create memory based on the query
      // The backend/AI will decide if this should become a memory
      try {
        await createMemory({ content: query });
        // Mark that memory was created for this response
        setResponses((prev) => {
          const updated = [...prev];
          const currentResponse = updated.find((r) => r.query === query);
          if (currentResponse) {
            currentResponse.memoryCreated = true;
          }
          return updated;
        });
      } catch (memErr) {
        // Memory creation is optional, don't break the flow
        console.log("Memory creation skipped or failed:", memErr);
      }
    } catch (err) {
      console.error("Completion failed:", err);

      // Check if it's an authentication error
      if (err instanceof Error && err.message.includes("401")) {
        console.log("Authentication error detected. Current session:", session);

        // Try refreshing the session
        try {
          await refreshUser();
          console.log("Session refreshed, please try again");
        } catch (refreshErr) {
          console.error("Failed to refresh session:", refreshErr);
        }
      }

      // Update the response with error
      setResponses((prev) => {
        const newResponses = [...prev];
        const lastResponse = newResponses[newResponses.length - 1];
        if (lastResponse?.isStreaming) {
          lastResponse.content =
            err instanceof Error && err.message.includes("401")
              ? "Authentication expired. Please refresh the page and try again."
              : "I apologize, but I encountered an error. Please try again.";
          lastResponse.isStreaming = false;
        }
        return newResponses;
      });
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      // Create a synthetic form event
      const syntheticEvent = {
        preventDefault: () => {},
      } as FormEvent<HTMLFormElement>;
      handleSendMessage(syntheticEvent);
    }
  };

  const clearChat = () => {
    setResponses([]);
    resetCompletion();
  };

  return (
    <div className="min-h-full flex flex-col relative">
      {/* Simple Header - only show when there are responses */}
      {responses.length > 0 && (
        <div className="flex-shrink-0 p-2 border-b border-gray-700/50">
          <div className="max-w-4xl mx-auto flex items-center justify-end">
            <Button
              onClick={clearChat}
              variant="ghost"
              className="text-xs px-2 py-1 text-gray-500 hover:text-gray-300"
            >
              Clear
            </Button>
          </div>
        </div>
      )}

      {/* Results/Responses Area */}
      <div className="flex-1 overflow-y-auto pb-24">
        <div className="max-w-4xl mx-auto p-3 space-y-3">
          {responses.length === 0 && (
            <div className="text-center text-gray-500 mt-8">
              <div className="text-4xl mb-3">💭</div>
              <p className="text-lg mb-1">What&apos;s on your mind?</p>
              <p className="text-sm">Ask any question or share your thoughts</p>
            </div>
          )}

          {responses.map((response) => (
            <div
              key={response.id}
              className="bg-gray-800/50 backdrop-blur-md rounded-xl border border-gray-700/50 overflow-hidden"
            >
              {/* Query Header */}
              <div className="bg-gradient-to-r from-orange-500/10 to-orange-600/10 border-b border-gray-700/30 p-3">
                <div className="flex items-start gap-2">
                  <div className="w-6 h-6 bg-gradient-to-br from-orange-400 to-orange-600 rounded-full flex items-center justify-center flex-shrink-0">
                    <span className="text-white text-xs">💭</span>
                  </div>
                  <div className="flex-1">
                    <p className="text-gray-200 font-medium text-sm">
                      {response.query}
                    </p>
                    <div className="flex items-center gap-2 mt-0.5 text-xs text-gray-400">
                      <span>{formatTime(response.timestamp)}</span>
                      {response.memoryCreated && (
                        <span className="bg-green-500/20 text-green-300 px-1.5 py-0.5 rounded-full text-xs">
                          💾 Saved as Memory
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Response Content */}
              <div className="p-4">
                {response.isStreaming && !response.content ? (
                  <div className="flex items-center gap-2">
                    <LoadingSpinner size="sm" variant="minimal" />
                    <span className="text-gray-400 text-sm">Thinking...</span>
                  </div>
                ) : (
                  <div className="text-gray-200 leading-relaxed text-sm">
                    {response.content || "No response yet"}
                    {response.isStreaming && response.content && (
                      <LoadingSpinner
                        size="sm"
                        variant="minimal"
                        className="inline-block ml-2"
                      />
                    )}
                  </div>
                )}
              </div>
            </div>
          ))}

          <div ref={responsesEndRef} />
        </div>
      </div>

      {/* Fixed Input at Bottom */}
      <div className="fixed bottom-0 left-0 right-0 p-3 bg-gradient-to-t from-gray-900/90 to-transparent backdrop-blur-sm">
        <div className="max-w-4xl mx-auto">
          <div className="bg-gray-800/50 backdrop-blur-xl rounded-xl border border-gray-700/50 shadow-2xl p-3">
            <div className="flex items-end gap-3">
              <form
                onSubmit={handleSendMessage}
                className="flex items-end gap-3 flex-1"
              >
                <textarea
                  value={inputMessage}
                  onChange={(e) => setInputMessage(e.target.value)}
                  onKeyDown={handleKeyPress}
                  placeholder={
                    isCompleting ? "Processing..." : "What's on your mind?"
                  }
                  disabled={isCompleting}
                  className="flex-1 bg-gray-700/50 border border-gray-600/50 rounded-lg px-3 py-2 text-gray-200 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-transparent resize-none"
                  rows={1}
                  style={{
                    minHeight: "42px",
                    maxHeight: "120px",
                    height: "auto",
                  }}
                  onInput={(e) => {
                    e.currentTarget.style.height = "auto";
                    e.currentTarget.style.height =
                      Math.min(e.currentTarget.scrollHeight, 120) + "px";
                  }}
                />
                <Button
                  type="submit"
                  disabled={!inputMessage.trim() || isCompleting}
                >
                  {isCompleting ? (
                    <span className="flex items-center gap-2">
                      <LoadingSpinner size="sm" variant="minimal" />
                      Processing...
                    </span>
                  ) : (
                    "Submit"
                  )}
                </Button>
              </form>
            </div>

            <div className="mt-2 text-xs text-gray-400 text-center">
              Press Enter to submit, Shift+Enter for new line
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
