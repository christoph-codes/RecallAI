"use client";

import { useMemo, useState } from "react";
import Button from "@/components/button";
import LoadingSpinner from "@/components/LoadingSpinner";
import { useAuthGuard } from "@/hooks/useAuthGuard";
import {
  useMemoryList,
  useMemoryDetail,
  useDeleteMemory,
} from "@/queries";

const formatTimestamp = (value: string) =>
  new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });

const minMetadata = (metadata: Record<string, unknown> | null) => {
  if (!metadata || Object.keys(metadata).length === 0) return null;
  return metadata;
};

const MemoriesPage = () => {
  const { loading } = useAuthGuard({ requireAuth: true });
  const {
    memories,
    page,
    pageSize,
    totalCount,
    isLoading,
    error,
    goToPage,
    refetch,
  } = useMemoryList({ pageSize: 10 });

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const {
    memory: selectedMemory,
    isLoading: isDetailLoading,
    error: detailError,
    refetch: refetchDetail,
  } = useMemoryDetail(selectedId, { enabled: !!selectedId });
  const {
    deleteMemoryById,
    isLoading: isDeleting,
    error: deleteError,
  } = useDeleteMemory();

  const maxPage = useMemo(() => {
    if (totalCount === 0) return 1;
    return Math.max(1, Math.ceil(totalCount / pageSize));
  }, [totalCount, pageSize]);

  const handleDelete = async (id: string) => {
    await deleteMemoryById(id);
    if (selectedId === id) {
      setSelectedId(null);
    }
    await refetch();
  };

  const handleSelect = (id: string) => {
    setSelectedId((current) => (current === id ? null : id));
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <LoadingSpinner
          size="lg"
          message="Loading memories..."
          variant="branded"
        />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900 text-gray-100">
      <div className="max-w-6xl mx-auto px-4 py-6 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold text-white">
              Memory library
            </h1>
            <p className="text-sm text-gray-400 mt-1">
              Browse and manage everything you have captured. Select a memory to
              inspect its details or remove it from your knowledge base.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              onClick={() => refetch()}
              disabled={isLoading}
            >
              Refresh
            </Button>
          </div>
        </div>

        {error && (
          <div className="text-sm text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
            {error.message}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-[1.4fr_1fr] gap-6">
          <div className="bg-gray-800/60 border border-gray-700/50 rounded-xl p-4 space-y-3">
            <div className="flex items-center justify-between text-sm text-gray-400">
              <span>
                Showing {(page - 1) * pageSize + 1}-
                {Math.min(page * pageSize, totalCount)} of {totalCount} memories
              </span>
              <span>Page {page} of {maxPage}</span>
            </div>

            <div className="space-y-3">
              {isLoading && (
                <div className="flex items-center gap-2 text-sm text-gray-400">
                  <LoadingSpinner size="sm" variant="minimal" />
                  <span>Loading memories...</span>
                </div>
              )}

              {!isLoading && memories.length === 0 && (
                <div className="text-sm text-gray-400 bg-gray-900/60 border border-gray-700/60 rounded-lg p-6 text-center">
                  No memories found yet. Create a few from the dashboard to see
                  them appear here.
                </div>
              )}

              {memories.map((memory) => {
                const isSelected = selectedId === memory.id;
                return (
                  <button
                    key={memory.id}
                    onClick={() => handleSelect(memory.id)}
                    className={`w-full text-left rounded-lg border px-4 py-3 transition ${
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
                          {formatTimestamp(memory.createdAt)}
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

            <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
              <Button
                variant="ghost"
                disabled={page === 1 || isLoading}
                onClick={() => goToPage(Math.max(1, page - 1))}
              >
                Previous
              </Button>
              <div className="text-xs text-gray-400">
                Page {page} of {maxPage}
              </div>
              <Button
                variant="ghost"
                disabled={page >= maxPage || isLoading}
                onClick={() => goToPage(Math.min(maxPage, page + 1))}
              >
                Next
              </Button>
            </div>
          </div>

          <div className="bg-gray-800/40 border border-gray-700/40 rounded-xl p-4 h-full">
            {selectedId === null && (
              <div className="h-full flex flex-col items-center justify-center text-sm text-gray-500 text-center px-4">
                Select a memory from the list to view its full details.
              </div>
            )}

            {selectedId !== null && (
              <div className="space-y-3">
                {(isDetailLoading || !selectedMemory) && (
                  <div className="flex items-center gap-2 text-sm text-gray-400">
                    <LoadingSpinner size="sm" variant="minimal" />
                    <span>Loading memory details...</span>
                  </div>
                )}

                {detailError && (
                  <div className="text-sm text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
                    {detailError.message}
                  </div>
                )}

                {selectedMemory && (
                  <>
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <h2 className="text-lg font-semibold text-white">
                          {selectedMemory.title ?? "Untitled memory"}
                        </h2>
                        <p className="text-xs text-gray-500">
                          Created {formatTimestamp(selectedMemory.createdAt)}
                        </p>
                        {selectedMemory.updatedAt !== selectedMemory.createdAt && (
                          <p className="text-xs text-gray-600">
                            Updated {formatTimestamp(selectedMemory.updatedAt)}
                          </p>
                        )}
                      </div>
                      <Button
                        variant="danger"
                        className="text-xs px-3 py-1"
                        onClick={async () => {
                          await handleDelete(selectedMemory.id);
                        }}
                        disabled={isDeleting}
                      >
                        {isDeleting ? "Deleting..." : "Delete"}
                      </Button>
                    </div>

                    <div className="text-sm text-gray-200 whitespace-pre-wrap bg-gray-900/60 border border-gray-700/60 rounded-lg p-3">
                      {selectedMemory.content}
                    </div>

                    {minMetadata(selectedMemory.metadata) && (
                      <div className="bg-gray-900/60 border border-gray-700/60 rounded-lg p-3 text-xs text-gray-400">
                        <p className="font-medium text-gray-300 mb-1">
                          Metadata
                        </p>
                        <pre className="whitespace-pre-wrap">
                          {JSON.stringify(selectedMemory.metadata, null, 2)}
                        </pre>
                      </div>
                    )}

                    <div className="flex items-center gap-2">
                      <Button
                        variant="secondary"
                        className="text-xs"
                        onClick={() => refetchDetail()}
                        disabled={isDetailLoading}
                      >
                        Refresh details
                      </Button>
                      {deleteError && (
                        <span className="text-xs text-red-300">
                          {deleteError.message}
                        </span>
                      )}
                    </div>
                  </>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default MemoriesPage;
