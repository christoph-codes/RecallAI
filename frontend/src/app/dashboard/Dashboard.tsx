"use client";

import Button from "@/components/button";
import Input from "@/components/input";
import { useAuthGuard } from "@/hooks/useAuthGuard";
import { useCreateMemory } from "@/queries";
import { FormEvent, useState } from "react";

interface ResultCard {
  id: string;
  query: string;
  result: string;
  timestamp: Date;
}

const Dashboard = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ResultCard[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const { createMemoryMutation: createMemory } = useCreateMemory();
  const [processingMessage, setProcessingMessage] = useState("");

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="text-xl">Loading...</div>
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!query.trim()) return;

    // Store the query message and clear input immediately
    const userMessage = query.trim();
    setQuery("");

    setIsProcessing(true);
    setProcessingMessage(userMessage);

    createMemory({ content: userMessage }).catch((err) => {
      console.error("Error creating memory:", err);
    });

    // Simulate processing
    setTimeout(() => {
      const newResult: ResultCard = {
        id: Date.now().toString(),
        query: userMessage,
        result: `This is a placeholder result for your request: "${userMessage}". The actual implementation would process this through your AI backend.`,
        timestamp: new Date(),
      };

      setResults((prev) => [newResult, ...prev]);
      setIsProcessing(false);
      setProcessingMessage("");
    }, 2000);
  };

  const removeResult = (id: string) => {
    setResults((prev) => prev.filter((result) => result.id !== id));
  };

  const copyResult = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  return (
    <div className="min-h-full flex flex-col relative">
      {/* Results Area */}
      <div className="flex-1 overflow-y-auto pb-32">
        {/* Processing State */}
        {isProcessing && (
          <div className="max-w-4xl mx-auto p-3">
            <div className="bg-gray-800/50 backdrop-blur-md rounded-2xl p-6 border border-white/50 shadow-lg">
              <div className="flex items-center gap-4">
                <div className="w-8 h-8 bg-gray-700/50 backdrop-blur-sm rounded-full flex items-center justify-center animate-pulse">
                  <span className="text-gray-300">🧠</span>
                </div>
                <div className="flex-1">
                  <div className="text-gray-200 font-medium mb-1">
                    Processing your request...
                  </div>
                  <div className="text-gray-400 text-sm mb-2">
                    &ldquo;{processingMessage}&rdquo;
                  </div>
                  <div className="text-gray-500 text-xs">
                    Analyzing and generating results
                  </div>
                </div>
                <div className="flex space-x-1">
                  <div
                    className="w-2 h-2 bg-gray-500 rounded-full animate-bounce"
                    style={{ animationDelay: "0ms" }}
                  ></div>
                  <div
                    className="w-2 h-2 bg-gray-500 rounded-full animate-bounce"
                    style={{ animationDelay: "150ms" }}
                  ></div>
                  <div
                    className="w-2 h-2 bg-gray-500 rounded-full animate-bounce"
                    style={{ animationDelay: "300ms" }}
                  ></div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Results */}
        <div className="space-y-4 max-w-4xl mx-auto p-3">
          {results.map((result) => (
            <div
              key={result.id}
              className="bg-gray-800/50 backdrop-blur-md rounded-2xl border border-gray-700/50 shadow-lg overflow-hidden"
            >
              <div className="p-4">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 bg-gray-700/50 backdrop-blur-sm rounded-full flex items-center justify-center border border-gray-600/30">
                      <span className="text-gray-300 text-sm">💭</span>
                    </div>
                    <div>
                      <div className="font-medium text-gray-200">Response</div>
                      <div className="text-gray-500 text-xs">
                        {result.timestamp.toLocaleTimeString()}
                      </div>
                    </div>
                  </div>
                  <button
                    onClick={() => removeResult(result.id)}
                    className="text-gray-500 hover:text-gray-300 transition-colors"
                  >
                    ✕
                  </button>
                </div>

                <div className="text-gray-300 text-sm mb-3 bg-gray-700/30 rounded-lg p-3 border border-gray-600/30">
                  {result.query}
                </div>

                <div className="text-gray-200 text-sm leading-relaxed mb-4">
                  {result.result}
                </div>

                <div className="flex gap-2">
                  <button
                    onClick={() => copyResult(result.result)}
                    className="bg-gray-700/50 hover:bg-gray-600/50 backdrop-blur-sm rounded-lg px-3 py-1.5 border border-gray-600/30 transition-all text-gray-300 hover:text-gray-100 text-xs"
                  >
                    📋 Copy
                  </button>
                  <button className="bg-gray-700/50 hover:bg-gray-600/50 backdrop-blur-sm rounded-lg px-3 py-1.5 border border-gray-600/30 transition-all text-gray-300 hover:text-gray-100 text-xs">
                    💾 Save
                  </button>
                  <button className="bg-gray-700/50 hover:bg-gray-600/50 backdrop-blur-sm rounded-lg px-3 py-1.5 border border-gray-600/30 transition-all text-gray-300 hover:text-gray-100 text-xs">
                    🔗 Share
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>

        {results.length === 0 && !isProcessing && (
          <div className="text-center text-gray-500 mt-12">
            <div className="text-4xl mb-4">💭</div>
            <p>Share any thought, question, or idea below</p>
          </div>
        )}
      </div>

      {/* Fixed Input at Bottom - Always Visible */}
      <div className="fixed bottom-0 left-0 right-0 p-4 bg-gradient-to-t from-gray-900/90 to-transparent backdrop-blur-sm">
        <div className="max-w-4xl mx-auto">
          <div className="bg-gray-800/50 backdrop-blur-xl rounded-2xl border border-gray-700/50 shadow-2xl p-4">
            <div className="flex items-end gap-3">
              <form
                onSubmit={handleSubmit}
                className="flex items-stretch flex-1 gap-2"
              >
                <Input
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="What's on your mind?"
                  //   className="w-full bg-transparent text-gray-200 placeholder-gray-400 resize-none focus:outline-none text-sm leading-relaxed min-h-[40px] max-h-32 py-2"
                  //   rows={2}
                />
                <Button
                  type="submit"
                  disabled={!query.trim() || isProcessing}
                  variant="glass"
                >
                  <span className="text-gray-200 font-medium">Submit</span>
                </Button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
