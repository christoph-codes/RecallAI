"use client";

import { FormEvent, useMemo, useState } from "react";
import Button from "@/components/button";
import Input from "@/components/input";
import LoadingSpinner from "@/components/LoadingSpinner";
import { useAuthGuard } from "@/hooks/useAuthGuard";
import {
  useMemorySearch,
  type MemorySearchResult,
} from "@/queries/useMemorySearch";

const formatTimestamp = (value: string) =>
  new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });

const SearchResultCard = ({
  result,
  onCopy,
}: {
  result: MemorySearchResult;
  onCopy: (text: string) => void;
}) => {
  return (
    <div className="bg-gray-800/60 border border-gray-700/60 rounded-xl p-4 space-y-3">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-semibold text-gray-200">
            {result.title ?? "Untitled memory"}
          </p>
          <p className="text-xs text-gray-500">
            {formatTimestamp(result.createdAt)}
          </p>
        </div>
        <div className="text-xs text-gray-400 flex flex-col items-end gap-1">
          <span>
            Score:{" "}
            <span className="font-semibold text-orange-300">
              {result.combinedScore.toFixed(4)}
            </span>
          </span>
          <span className="uppercase tracking-wide">
            {result.searchMethod}
          </span>
        </div>
      </div>

      <div className="text-sm text-gray-200 whitespace-pre-line">
        {result.content}
      </div>

      {result.metadata && Object.keys(result.metadata).length > 0 && (
        <div className="bg-gray-900/60 border border-gray-700/60 rounded-lg p-3 text-xs text-gray-400">
          <p className="font-medium text-gray-300 mb-1">Metadata</p>
          <pre className="whitespace-pre-wrap">
            {JSON.stringify(result.metadata, null, 2)}
          </pre>
        </div>
      )}

      <div className="flex items-center justify-end gap-2">
        <Button
          variant="ghost"
          className="text-xs px-3 py-1"
          onClick={() => onCopy(result.content)}
        >
          Copy
        </Button>
      </div>
    </div>
  );
};

const SearchPage = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const { data, isLoading, error, search, reset } = useMemorySearch();

  const [query, setQuery] = useState("");
  const [limit, setLimit] = useState(10);
  const [threshold, setThreshold] = useState(0.7);
  const [useHyde, setUseHyde] = useState(false);
  const [copyFeedback, setCopyFeedback] = useState<string | null>(null);

  const canSearch = query.trim().length > 0 && !isLoading;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!canSearch) return;

    await search({
      query: query.trim(),
      limit,
      threshold,
      useHyde,
    });
  };

  const handleReset = () => {
    setQuery("");
    reset();
  };

  const handleCopy = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopyFeedback("Copied to clipboard");
      setTimeout(() => setCopyFeedback(null), 2000);
    } catch {
      setCopyFeedback("Unable to copy");
      setTimeout(() => setCopyFeedback(null), 2000);
    }
  };

  const results = useMemo(
    () => data?.results ?? [],
    [data?.results]
  );

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <LoadingSpinner
          size="lg"
          message="Loading search..."
          variant="branded"
        />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900 text-gray-100">
      <div className="max-w-5xl mx-auto px-4 py-8 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold text-white">
              Search your memories
            </h1>
            <p className="text-sm text-gray-400 mt-1">
              Find relevant memories using vector search. Toggle HyDE for
              hypothetical document expansion when you need deeper recall.
            </p>
          </div>
          {copyFeedback && (
            <span className="text-xs text-orange-300 bg-orange-500/10 border border-orange-500/30 rounded-full px-3 py-1">
              {copyFeedback}
            </span>
          )}
        </div>

        <form
          onSubmit={handleSubmit}
          className="bg-gray-800/60 border border-gray-700/50 rounded-xl p-4 space-y-4"
        >
          <div className="flex flex-col gap-3">
            <label className="text-sm font-medium text-gray-300">
              Search query
            </label>
            <textarea
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              rows={3}
              placeholder="What would you like to recall?"
              className="w-full bg-gray-900/60 border border-gray-700/60 rounded-lg px-3 py-2 text-gray-200 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-orange-500/60"
              disabled={isLoading}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input
              label="Result limit"
              value={limit.toString()}
              type="number"
              onChange={(event) =>
                setLimit(
                  Math.min(
                    50,
                    Math.max(1, Number.parseInt(event.target.value, 10) || 1)
                  )
                )
              }
              placeholder="10"
              disabled={isLoading}
            />

            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium text-gray-300">
                Similarity threshold ({threshold.toFixed(2)})
              </label>
              <input
                type="range"
                min={0}
                max={1}
                step={0.05}
                value={threshold}
                onChange={(event) =>
                  setThreshold(Number.parseFloat(event.target.value))
                }
                className="w-full accent-orange-500"
                disabled={isLoading}
              />
              <p className="text-xs text-gray-500">
                Higher threshold shows only closer matches.
              </p>
            </div>

            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium text-gray-300">
                HyDE expansion
              </label>
              <button
                type="button"
                onClick={() => setUseHyde((value) => !value)}
                className={`w-full px-3 py-2 rounded-lg border transition ${
                  useHyde
                    ? "bg-orange-500/20 border-orange-500/40 text-orange-300"
                    : "bg-gray-900/60 border-gray-700/60 text-gray-300"
                }`}
                disabled={isLoading}
              >
                {useHyde ? "HyDE enabled" : "HyDE disabled"}
              </button>
              <p className="text-xs text-gray-500">
                Generate a hypothetical document to improve search recall.
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <Button type="submit" disabled={!canSearch}>
              {isLoading ? (
                <span className="flex items-center gap-2 justify-center">
                  <LoadingSpinner size="sm" variant="minimal" />
                  Searching...
                </span>
              ) : (
                "Search memories"
              )}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={handleReset}
              disabled={isLoading && !data}
            >
              Clear
            </Button>
            {data?.hydeUsed && (
              <span className="text-xs text-purple-300 bg-purple-500/20 border border-purple-500/40 px-3 py-1 rounded-full">
                HyDE applied
              </span>
            )}
          </div>

          {error && (
            <div className="text-sm text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
              {error.message}
            </div>
          )}
        </form>

        <div className="space-y-4">
          {isLoading && !data && (
            <div className="flex items-center gap-2 text-gray-400 text-sm">
              <LoadingSpinner size="sm" variant="minimal" />
              <span>Searching your memories…</span>
            </div>
          )}

          {!isLoading && data && results.length === 0 && (
            <div className="bg-gray-800/40 border border-gray-700/40 rounded-xl p-8 text-center text-gray-400 text-sm">
              <p>No memories found yet. Try refining your query or lowering the threshold.</p>
            </div>
          )}

          {results.length > 0 && (
            <div className="space-y-3">
              <div className="flex items-center justify-between text-sm text-gray-400">
                <span>
                  Showing {results.length} of {data?.resultCount ?? 0} results
                  in {data?.executionTimeMs ?? 0} ms
                </span>
              </div>

              <div className="space-y-3">
                {results.map((result) => (
                  <SearchResultCard
                    key={result.id}
                    result={result}
                    onCopy={handleCopy}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default SearchPage;
