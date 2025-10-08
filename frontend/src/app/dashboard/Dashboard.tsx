"use client";

import Button from "@/components/button";
import LoadingSpinner from "@/components/LoadingSpinner";
import { useAuthGuard } from "@/hooks/useAuthGuard";
import {
  useCreateMemory,
  useCompletion,
  useMemoryList,
  useDeleteMemory,
} from "@/queries";
import { useUser } from "@/contexts/UserContext";
import Link from "next/link";
import {
  FormEvent,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

type Response = {
  id: string;
  query: string;
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
  memoryCreated?: boolean;
};

const formatTime = (date: Date) =>
  new Intl.DateTimeFormat("en-US", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  }).format(date);

const formatMemoryDate = (value: string) =>
  new Date(value).toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });

const Dashboard = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const { refreshUser } = useUser();

  const [responses, setResponses] = useState<Response[]>([]);
  const [inputMessage, setInputMessage] = useState("");
  const responsesEndRef = useRef<HTMLDivElement>(null);

  const {
    createMemoryMutation: createMemory,
    error: createMemoryError,
  } = useCreateMemory();

  const {
    complete,
    isLoading: isCompleting,
    reset: resetCompletion,
  } = useCompletion({
    streaming: true,
    onChunk: (chunk) => {
      setResponses((prev) => {
        const updated = [...prev];
        const last = updated[updated.length - 1];
        if (last?.isStreaming) {
          last.content = (last.content || "") + chunk;
        }
        return updated;
      });
    },
    onComplete: (fullResponse) => {
      setResponses((prev) => {
        const updated = [...prev];
        const last = updated[updated.length - 1];
        if (last?.isStreaming) {
          last.isStreaming = false;
          last.content = fullResponse;
        }
        return updated;
      });
    },
  });

  const {
    memories,
    isLoading: isMemoriesLoading,
    error: memoriesError,
    refetch: refetchMemories,
  } = useMemoryList({ pageSize: 6 });

  const {
    deleteMemoryById,
    isLoading: isDeletingMemory,
    error: deleteMemoryError,
  } = useDeleteMemory();

  const [selectedMemoryId, setSelectedMemoryId] = useState<string | null>(null);
  const [isMemoryPanelVisible, setIsMemoryPanelVisible] = useState(false);

  const selectedMemory = useMemo(
    () => memories.find((memory) => memory.id === selectedMemoryId) ?? null,
    [memories, selectedMemoryId]
  );

  useEffect(() => {
    responsesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [responses]);

  useEffect(() => {
    if (!selectedMemoryId) {
      setIsMemoryPanelVisible(false);
    }
  }, [selectedMemoryId]);

  useEffect(() => {
    if (selectedMemoryId && !selectedMemory && !isMemoriesLoading) {
      setSelectedMemoryId(null);
    }
  }, [selectedMemoryId, selectedMemory, isMemoriesLoading]);

  const openMemoryPanel = (memoryId: string) => {
    setSelectedMemoryId(memoryId);
    setIsMemoryPanelVisible(true);
  };

  const closeMemoryPanel = () => {
    setIsMemoryPanelVisible(false);
    setSelectedMemoryId(null);
  };

  const handleDeleteMemory = async (id: string) => {
    try {
      await deleteMemoryById(id);
      await refetchMemories();
      closeMemoryPanel();
    } catch (err) {
      console.error("Failed to delete memory:", err);
    }
  };

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

  const handleSendMessage = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!inputMessage.trim() || isCompleting) return;

    const query = inputMessage.trim();
    const responseId = Date.now().toString();

    const newResponse: Response = {
      id: responseId,
      query,
      content: "",
      timestamp: new Date(),
      isStreaming: true,
    };

    setResponses((prev) => [...prev, newResponse]);
    setInputMessage("");

    try {
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

      try {
        const created = await createMemory({ content: query });
        setResponses((prev) =>
          prev.map((response) =>
            response.id === responseId
              ? { ...response, memoryCreated: true }
              : response
          )
        );
        try {
          await refetchMemories();
        } catch (refreshErr) {
          console.error("Failed to refresh memories:", refreshErr);
        }
        openMemoryPanel(created.id);
      } catch (memoryError) {
        console.log("Memory creation skipped or failed:", memoryError);
      }
    } catch (err) {
      console.error("Completion failed:", err);

      if (err instanceof Error && err.message.includes("401")) {
        try {
          await refreshUser();
          console.log("Session refreshed, please try again");
        } catch (refreshErr) {
          console.error("Failed to refresh session:", refreshErr);
        }
      }

      setResponses((prev) => {
        const updated = [...prev];
        const last = updated[updated.length - 1];
        if (last?.isStreaming) {
          last.content =
            err instanceof Error && err.message.includes("401")
              ? "Authentication expired. Please refresh the page and try again."
              : "I encountered an error while processing that. Please try again.";
          last.isStreaming = false;
        }
        return updated;
      });
    }
  };

  const handleKeyPress = (event: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
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
      {responses.length > 0 && (
        <div className="flex-shrink-0 p-2 border-b border-gray-700/50">
          <div className="max-w-5xl mx-auto flex items-center justify-end">
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

      <div className="flex-1 overflow-y-auto pb-24">
        <div className="max-w-5xl mx-auto p-4 space-y-6">
          <section className="bg-gray-800/40 border border-gray-700/40 rounded-xl p-4 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-semibold text-gray-200 uppercase tracking-wide">
                  Recent memories
                </h2>
                <p className="text-xs text-gray-500">
                  Click a memory to revisit it instantly.
                </p>
              </div>
              <div className="flex items-center gap-3">
                <Button
                  variant="ghost"
                  className="text-xs px-3 py-1"
                  onClick={() => refetchMemories()}
                  disabled={isMemoriesLoading}
                >
                  Refresh
                </Button>
                <Link
                  href="/memories"
                  className="text-xs text-orange-300 hover:text-orange-200"
                >
                  View library →
                </Link>
              </div>
            </div>

            {memoriesError && (
              <div className="text-xs text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
                {memoriesError.message}
              </div>
            )}

            {createMemoryError && (
              <div className="text-xs text-yellow-300 bg-yellow-500/10 border border-yellow-500/30 rounded-lg px-3 py-2">
                Failed to store memory: {createMemoryError.message}
              </div>
            )}

            <div className="grid gap-3 sm:grid-cols-2">
              {isMemoriesLoading && (
                <div className="flex items-center gap-2 text-sm text-gray-400">
                  <LoadingSpinner size="sm" variant="minimal" />
                  <span>Loading memories…</span>
                </div>
              )}

              {!isMemoriesLoading && memories.length === 0 && (
                <div className="text-sm text-gray-400 bg-gray-900/60 border border-gray-700/60 rounded-lg p-4 text-center">
                  No memories saved yet. Ask RecallAI something new to capture
                  it here.
                </div>
              )}

              {memories.map((memory) => {
                const isSelected = memory.id === selectedMemoryId;
                return (
                  <button
                    key={memory.id}
                    onClick={() => openMemoryPanel(memory.id)}
                    className={`text-left rounded-lg border px-4 py-3 transition ${
                      isSelected
                        ? "border-orange-500/60 bg-orange-500/10"
                        : "border-gray-700/50 bg-gray-900/40 hover:border-orange-500/40 hover:bg-gray-900/60"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold text-gray-200">
                          {memory.title ?? "Untitled memory"}
                        </p>
                        <p className="text-xs text-gray-500">
                          {formatMemoryDate(memory.createdAt)}
                        </p>
                      </div>
                      <span className="text-xs uppercase tracking-wide text-gray-400">
                        {memory.contentType}
                      </span>
                    </div>
                    <p className="mt-3 text-sm text-gray-300 line-clamp-3">
                      {memory.content}
                    </p>
                  </button>
                );
              })}
            </div>
          </section>

          <section className="space-y-3">
            {responses.length === 0 && (
              <div className="text-center text-gray-500 mt-8">
                <div className="text-4xl mb-3">RA</div>
                <p className="text-lg mb-1">What&apos;s on your mind?</p>
                <p className="text-sm">
                  Ask any question or share your thoughts.
                </p>
              </div>
            )}

            {responses.map((response) => (
              <div
                key={response.id}
                className="bg-gray-800/50 backdrop-blur-md rounded-xl border border-gray-700/50 overflow-hidden"
              >
                <div className="bg-gradient-to-r from-orange-500/10 to-orange-600/10 border-b border-gray-700/30 p-3">
                  <div className="flex items-start gap-2">
                    <div className="w-6 h-6 bg-gradient-to-br from-orange-400 to-orange-600 rounded-full flex items-center justify-center flex-shrink-0">
                      <span className="text-white text-xs">RA</span>
                    </div>
                    <div className="flex-1">
                      <p className="text-gray-200 font-medium text-sm">
                        {response.query}
                      </p>
                      <div className="flex items-center gap-2 mt-0.5 text-xs text-gray-400">
                        <span>{formatTime(response.timestamp)}</span>
                        {response.memoryCreated && (
                          <span className="bg-green-500/20 text-green-300 px-1.5 py-0.5 rounded-full text-xs">
                            Saved as memory
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                <div className="p-4">
                  {response.isStreaming && !response.content ? (
                    <div className="flex items-center gap-2">
                      <LoadingSpinner size="sm" variant="minimal" />
                      <span className="text-gray-400 text-sm">Thinking...</span>
                    </div>
                  ) : (
                    <div className="text-gray-200 leading-relaxed text-sm whitespace-pre-wrap">
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
          </section>
        </div>
      </div>

      <div className="fixed bottom-0 left-0 right-0 p-3 bg-gradient-to-t from-gray-900/90 to-transparent backdrop-blur-sm">
        <div className="max-w-5xl mx-auto">
          <div className="bg-gray-800/50 backdrop-blur-xl rounded-xl border border-gray-700/50 shadow-2xl p-3">
            <div className="flex items-end gap-3">
              <form
                onSubmit={handleSendMessage}
                className="flex items-end gap-3 flex-1"
              >
                <textarea
                  value={inputMessage}
                  onChange={(event) => setInputMessage(event.target.value)}
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
                  onInput={(event) => {
                    event.currentTarget.style.height = "auto";
                    const nextHeight = Math.min(
                      event.currentTarget.scrollHeight,
                      120
                    );
                    event.currentTarget.style.height = `${nextHeight}px`;
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

      {isMemoryPanelVisible && selectedMemory && (
        <div className="fixed inset-0 z-40 flex">
          <button
            className="flex-1 bg-black/40 backdrop-blur-sm"
            onClick={closeMemoryPanel}
            aria-label="Close memory panel"
          />
          <div className="w-full max-w-md bg-gray-900 border-l border-gray-700 h-full shadow-2xl flex flex-col">
            <div className="flex items-start justify-between p-5 border-b border-gray-800">
              <div>
                <h2 className="text-lg font-semibold text-white">
                  {selectedMemory.title ?? "Untitled memory"}
                </h2>
                <p className="text-xs text-gray-500">
                  Stored {formatMemoryDate(selectedMemory.createdAt)}
                </p>
                {selectedMemory.updatedAt !== selectedMemory.createdAt && (
                  <p className="text-xs text-gray-600">
                    Updated {formatMemoryDate(selectedMemory.updatedAt)}
                  </p>
                )}
              </div>
              <button
                onClick={closeMemoryPanel}
                className="text-gray-400 hover:text-white"
                aria-label="Close memory detail"
              >
                ✕
              </button>
            </div>
            <div className="flex-1 overflow-y-auto p-5 space-y-4">
              <div className="text-sm text-gray-200 whitespace-pre-wrap bg-gray-800/50 border border-gray-700/60 rounded-lg p-3">
                {selectedMemory.content}
              </div>
              {selectedMemory.metadata &&
                Object.keys(selectedMemory.metadata).length > 0 && (
                  <div className="bg-gray-800/40 border border-gray-700/40 rounded-lg p-3 text-xs text-gray-400">
                    <p className="font-medium text-gray-300 mb-2">Metadata</p>
                    <pre className="whitespace-pre-wrap">
                      {JSON.stringify(selectedMemory.metadata, null, 2)}
                    </pre>
                  </div>
                )}
              {deleteMemoryError && (
                <div className="text-xs text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
                  {deleteMemoryError.message}
                </div>
              )}
            </div>
            <div className="p-5 border-t border-gray-800 flex items-center justify-between gap-3">
              <Button
                variant="secondary"
                className="text-xs"
                onClick={closeMemoryPanel}
              >
                Close
              </Button>
              <Button
                variant="danger"
                className="text-xs"
                onClick={() => handleDeleteMemory(selectedMemory.id)}
                disabled={isDeletingMemory}
              >
                {isDeletingMemory ? "Deleting..." : "Delete memory"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Dashboard;
