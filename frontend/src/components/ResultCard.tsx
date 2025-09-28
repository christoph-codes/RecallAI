"use client";

export type Memory = {
  id: string;
  title: string | null;
  content: string;
  contentType: string;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
};

export type ResultCardProps = {
  memory: Memory;
  query: string;
  result: string;
  onRemove: (id: string) => void;
  onCopy: (text: string) => void;
  isStreaming?: boolean;
};

const ResultCard = ({
  memory,
  query,
  result,
  onRemove,
  onCopy,
  isStreaming = false,
}: ResultCardProps) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString();
  };

  return (
    <div className="bg-gray-800/50 backdrop-blur-md rounded-2xl border border-gray-700/50 shadow-lg overflow-hidden">
      <div className="p-4">
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-gray-700/50 backdrop-blur-sm rounded-full flex items-center justify-center border border-gray-600/30">
              <span className="text-gray-300 text-sm">💭</span>
            </div>
            <div>
              <div className="font-medium text-gray-200">
                {memory.title || "Response"}
              </div>
              <div className="text-gray-500 text-xs">
                {formatDate(memory.createdAt)}
              </div>
              {memory.contentType && memory.contentType !== "text" && (
                <div className="text-gray-400 text-xs capitalize">
                  {memory.contentType}
                </div>
              )}
            </div>
          </div>
          <button
            onClick={() => onRemove(memory.id)}
            className="text-gray-500 hover:text-gray-300 transition-colors"
            aria-label="Remove result"
          >
            ✕
          </button>
        </div>

        {/* User Query */}
        <div className="text-gray-300 text-sm mb-3 bg-gray-700/30 rounded-lg p-3 border border-gray-600/30">
          {query}
        </div>

        {/* AI Response */}
        <div className="text-gray-200 text-sm leading-relaxed mb-4">
          {result}
          {isStreaming && (
            <span className="inline-block w-2 h-4 bg-orange-400 ml-2 animate-pulse" />
          )}
        </div>

        {/* Metadata (if exists) */}
        {Object.keys(memory.metadata).length > 0 && (
          <div className="text-gray-400 text-xs mb-3 bg-gray-700/20 rounded-lg p-2 border border-gray-600/20">
            <div className="font-medium mb-1">Metadata:</div>
            <pre className="whitespace-pre-wrap">
              {JSON.stringify(memory.metadata, null, 2)}
            </pre>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex gap-2">
          <button
            onClick={() => onCopy(result)}
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
  );
};

export default ResultCard;
