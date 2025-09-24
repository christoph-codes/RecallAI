"use client";

import { useAuthGuard } from "@/hooks/useAuthGuard";
import { useState } from "react";

type IntentType = "ask" | "analyze" | "create" | "brainstorm" | "code";

interface ResultCard {
  id: string;
  type: IntentType;
  query: string;
  result: string;
  timestamp: Date;
}

const Dashboard = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<ResultCard[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);

  const intents = {
    ask: {
      icon: "❓",
      label: "Ask",
      color: "from-blue-600/10 to-blue-700/10",
      borderColor: "border-blue-500/20",
      textColor: "text-blue-300",
      placeholder:
        "Ask me anything... What would you like to know or understand?",
    },
    analyze: {
      icon: "📊",
      label: "Analyze",
      color: "from-green-600/10 to-green-700/10",
      borderColor: "border-green-500/20",
      textColor: "text-green-300",
      placeholder: "Paste data, text, or describe what you need analyzed...",
    },
    create: {
      icon: "✨",
      label: "Create",
      color: "from-gray-600/10 to-gray-700/10",
      borderColor: "border-gray-500/20",
      textColor: "text-gray-300",
      placeholder:
        "Describe what you want to create... content, ideas, plans...",
    },
    brainstorm: {
      icon: "💡",
      label: "Brainstorm",
      color: "from-purple-600/10 to-purple-700/10",
      borderColor: "border-purple-500/20",
      textColor: "text-purple-300",
      placeholder: "Share your challenge or topic for creative exploration...",
    },
    code: {
      icon: "⚡",
      label: "Code",
      color: "from-indigo-600/10 to-indigo-700/10",
      borderColor: "border-indigo-500/20",
      textColor: "text-indigo-300",
      placeholder:
        "Describe the code, function, or technical solution you need...",
    },
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="text-xl">Loading...</div>
        </div>
      </div>
    );
  }

  const handleSubmit = async () => {
    if (!query.trim()) return;

    setIsProcessing(true);

    // Auto-detect intent based on query content (simplified logic)
    const detectIntent = (text: string): IntentType => {
      const lowerText = text.toLowerCase();
      if (
        lowerText.includes("code") ||
        lowerText.includes("function") ||
        lowerText.includes("programming")
      )
        return "code";
      if (
        lowerText.includes("analyze") ||
        lowerText.includes("data") ||
        lowerText.includes("review")
      )
        return "analyze";
      if (
        lowerText.includes("create") ||
        lowerText.includes("write") ||
        lowerText.includes("make")
      )
        return "create";
      if (
        lowerText.includes("brainstorm") ||
        lowerText.includes("ideas") ||
        lowerText.includes("think")
      )
        return "brainstorm";
      return "ask"; // default
    };

    const detectedIntent = detectIntent(query);

    // Simulate processing
    setTimeout(() => {
      const newResult: ResultCard = {
        id: Date.now().toString(),
        type: detectedIntent,
        query: query.trim(),
        result: `This is a placeholder result for your ${detectedIntent} request: "${query.trim()}". The actual implementation would process this through your AI backend.`,
        timestamp: new Date(),
      };

      setResults((prev) => [newResult, ...prev]);
      setQuery(""); // Keep input box open but clear the text
      setIsProcessing(false);
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
          <div className="max-w-4xl mx-auto mb-6 px-6">
            <div className="bg-gray-800/50 backdrop-blur-md rounded-2xl p-6 border border-gray-700/50 shadow-lg">
              <div className="flex items-center gap-4">
                <div className="w-8 h-8 bg-gray-700/50 backdrop-blur-sm rounded-full flex items-center justify-center animate-pulse">
                  <span className="text-gray-300">🧠</span>
                </div>
                <div className="flex-1">
                  <div className="text-gray-200 font-medium mb-1">
                    Processing your request...
                  </div>
                  <div className="text-gray-400 text-sm">
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
        <div className="space-y-4 max-w-4xl mx-auto px-6">
          {results.map((result) => (
            <div
              key={result.id}
              className="bg-gray-800/50 backdrop-blur-md rounded-2xl border border-gray-700/50 shadow-lg overflow-hidden"
            >
              <div className="p-4">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div
                      className={`w-8 h-8 bg-gradient-to-r ${
                        intents[result.type].color
                      } backdrop-blur-sm rounded-full flex items-center justify-center border ${
                        intents[result.type].borderColor
                      }`}
                    >
                      <span className="text-gray-300 text-sm">
                        {intents[result.type].icon}
                      </span>
                    </div>
                    <div>
                      <div
                        className={`font-medium ${
                          intents[result.type].textColor
                        }`}
                      >
                        {intents[result.type].label}
                      </div>
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
              <div className="flex-1">
                <textarea
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="What's on your mind? Ask questions, request analysis, create content, brainstorm ideas, or get help with code..."
                  className="w-full bg-transparent text-gray-200 placeholder-gray-400 resize-none focus:outline-none text-sm leading-relaxed min-h-[40px] max-h-32 py-2"
                  rows={2}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      handleSubmit();
                    }
                  }}
                />
              </div>
              <button
                onClick={handleSubmit}
                disabled={!query.trim() || isProcessing}
                className="bg-gray-700/50 hover:bg-gray-600/50 backdrop-blur-sm rounded-xl px-6 py-3 border border-gray-600/30 transition-all duration-200 hover:scale-105 focus:outline-none focus:ring-2 focus:ring-orange-500/50 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100"
              >
                <span className="text-gray-200 font-medium">Submit</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
