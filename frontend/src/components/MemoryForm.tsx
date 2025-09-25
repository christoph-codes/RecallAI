import { useState, FormEvent, useEffect } from "react";
import { useCreateMemory, useCompletion } from "@/queries";
import Button from "@/components/button";
import Input from "@/components/input";

export type Memory = {
  id: string;
  title: string | null;
  content: string;
  contentType: string;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
};

type MemoryFormProps = {
  onMemoryCreated?: (memory: Memory) => void;
  onCancel?: () => void;
  initialContent?: string;
};

const MemoryForm = ({
  onMemoryCreated,
  onCancel,
  initialContent = "",
}: MemoryFormProps) => {
  const [content, setContent] = useState(initialContent);
  const [title, setTitle] = useState("");
  const [contentType, setContentType] = useState<
    "text" | "document" | "note" | "conversation"
  >("text");
  const [useAIEnhancement, setUseAIEnhancement] = useState(true);

  const {
    createMemoryMutation,
    isLoading: isCreatingMemory,
    error: memoryError,
  } = useCreateMemory();
  const {
    complete,
    data: enhancementData,
    isLoading: isEnhancing,
    error: enhancementError,
    reset: resetEnhancement,
  } = useCompletion();

  const [enhancedTitle, setEnhancedTitle] = useState<string | null>(null);
  const [enhancementSuggestions, setEnhancementSuggestions] = useState<
    string | null
  >(null);

  const handleAIEnhancement = async () => {
    if (!content.trim()) return;

    resetEnhancement();

    const enhancementPrompt = `Please analyze this memory content and provide:
1. A concise, descriptive title (max 50 characters)
2. Enhancement suggestions to make it more searchable and valuable

Memory content: "${content}"

Please respond in this exact format:
TITLE: [your suggested title]
SUGGESTIONS: [your suggestions for improving the memory]`;

    try {
      await complete({
        message: enhancementPrompt,
        configuration: {
          temperature: 0.3, // Lower temperature for more consistent formatting
          maxTokens: 200,
        },
      });
    } catch (err) {
      console.error("AI Enhancement failed:", err);
    }
  };

  // Parse AI enhancement response
  useEffect(() => {
    if (enhancementData) {
      const lines = enhancementData.split("\n");
      let newTitle = "";
      let newSuggestions = "";

      for (const line of lines) {
        if (line.startsWith("TITLE:")) {
          newTitle = line.replace("TITLE:", "").trim();
        } else if (line.startsWith("SUGGESTIONS:")) {
          newSuggestions = line.replace("SUGGESTIONS:", "").trim();
        }
      }

      if (newTitle) {
        setEnhancedTitle(newTitle);
        if (!title) {
          // Only auto-fill if user hasn't typed anything
          setTitle(newTitle);
        }
      }
      if (newSuggestions) {
        setEnhancementSuggestions(newSuggestions);
      }
    }
  }, [enhancementData, title]);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!content.trim()) return;

    try {
      const memoryData = await createMemoryMutation({
        title: title.trim() || undefined,
        content: content.trim(),
        contentType,
        metadata: {
          aiEnhanced: useAIEnhancement,
          enhancedTitle: enhancedTitle,
          enhancementSuggestions: enhancementSuggestions,
        },
      });

      // Call the callback with the created memory
      if (onMemoryCreated && memoryData) {
        onMemoryCreated(memoryData);
      }

      // Reset form
      setContent("");
      setTitle("");
      setEnhancedTitle(null);
      setEnhancementSuggestions(null);
      resetEnhancement();
    } catch (err) {
      console.error("Failed to create memory:", err);
    }
  };

  return (
    <div className="bg-gray-800/50 backdrop-blur-md rounded-2xl border border-gray-700/50 shadow-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-200">Create Memory</h3>
        {onCancel && (
          <button
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-200 text-xl"
          >
            ✕
          </button>
        )}
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Content Input */}
        <div>
          <label
            htmlFor="content"
            className="block text-sm font-medium text-gray-300 mb-2"
          >
            Content *
          </label>
          <textarea
            id="content"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="What do you want to remember?"
            className="w-full bg-gray-700/50 border border-gray-600/50 rounded-lg px-4 py-3 text-gray-200 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-transparent resize-vertical min-h-[100px]"
            required
          />
        </div>

        {/* AI Enhancement */}
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-gray-300">
            <input
              type="checkbox"
              checked={useAIEnhancement}
              onChange={(e) => setUseAIEnhancement(e.target.checked)}
              className="rounded bg-gray-700 border-gray-600 text-orange-500 focus:ring-orange-500"
            />
            Use AI Enhancement
          </label>
          {useAIEnhancement && content.trim() && (
            <Button
              type="button"
              onClick={handleAIEnhancement}
              disabled={isEnhancing}
              variant="ghost"
            >
              {isEnhancing ? "Enhancing..." : "✨ Enhance"}
            </Button>
          )}
        </div>

        {/* AI Enhancement Results */}
        {enhancedTitle && (
          <div className="bg-orange-500/10 border border-orange-500/20 rounded-lg p-3">
            <div className="text-sm text-orange-300 font-medium mb-1">
              AI Suggested Title:
            </div>
            <div className="text-gray-200 text-sm">{enhancedTitle}</div>
          </div>
        )}

        {enhancementSuggestions && (
          <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-3">
            <div className="text-sm text-blue-300 font-medium mb-1">
              Enhancement Suggestions:
            </div>
            <div className="text-gray-200 text-sm">
              {enhancementSuggestions}
            </div>
          </div>
        )}

        {/* Title Input */}
        <div>
          <Input
            label="Title (Optional)"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Give your memory a title"
          />
        </div>

        {/* Content Type */}
        <div>
          <label
            htmlFor="contentType"
            className="block text-sm font-medium text-gray-300 mb-2"
          >
            Type
          </label>
          <select
            id="contentType"
            value={contentType}
            onChange={(e) =>
              setContentType(e.target.value as typeof contentType)
            }
            className="w-full bg-gray-700/50 border border-gray-600/50 rounded-lg px-4 py-2 text-gray-200 focus:outline-none focus:ring-2 focus:ring-orange-500/50 focus:border-transparent"
          >
            <option value="text">Text</option>
            <option value="note">Note</option>
            <option value="document">Document</option>
            <option value="conversation">Conversation</option>
          </select>
        </div>

        {/* Error Messages */}
        {(memoryError || enhancementError) && (
          <div className="text-red-400 text-sm">
            {memoryError?.message || enhancementError?.message}
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-3 pt-2">
          <Button
            type="submit"
            disabled={!content.trim() || isCreatingMemory || isEnhancing}
            className="flex-1"
          >
            {isCreatingMemory ? "Creating..." : "Create Memory"}
          </Button>
          {onCancel && (
            <Button type="button" onClick={onCancel} variant="ghost">
              Cancel
            </Button>
          )}
        </div>
      </form>
    </div>
  );
};

export default MemoryForm;
