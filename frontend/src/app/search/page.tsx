"use client";

import Button from "@/components/button";
import LoadingSpinner from "@/components/LoadingSpinner";
import Link from "next/link";
import { useAuthGuard } from "@/hooks/useAuthGuard";

const SearchPage = () => {
  const { loading } = useAuthGuard({ requireAuth: true });

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
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 flex items-center justify-center p-4">
      <div className="max-w-2xl mx-auto text-center">
        {/* Coming Soon Card */}
        <div className="bg-gray-800/50 backdrop-blur-md rounded-2xl border border-gray-700/50 p-8 shadow-2xl">
          {/* Icon */}
          <div className="w-24 h-24 mx-auto mb-6 bg-gradient-to-br from-orange-500/20 to-purple-600/20 rounded-full flex items-center justify-center">
            <span className="text-4xl">🔍</span>
          </div>

          {/* Title */}
          <h1 className="text-3xl font-bold text-white mb-4">
            Enhanced Search
          </h1>

          {/* Description */}
          <p className="text-gray-300 text-lg mb-2">Coming Soon to RecallAI</p>
          <p className="text-gray-400 mb-8 leading-relaxed">
            We&apos;re working on an intelligent search experience that will
            help you find exactly what you&apos;re looking for across all your
            memories and conversations. This premium feature will include
            enhanced query processing, semantic search, and smart suggestions.
          </p>

          {/* Features Preview */}
          <div className="grid md:grid-cols-2 gap-4 mb-8 text-left">
            <div className="bg-gray-700/30 rounded-lg p-4">
              <div className="flex items-center gap-3 mb-2">
                <span className="text-lg">✨</span>
                <h3 className="font-medium text-white">
                  Smart Query Enhancement
                </h3>
              </div>
              <p className="text-sm text-gray-400">
                Your search intent will be understood and queries enhanced
                automatically.
              </p>
            </div>

            <div className="bg-gray-700/30 rounded-lg p-4">
              <div className="flex items-center gap-3 mb-2">
                <span className="text-lg">🔍</span>
                <h3 className="font-medium text-white">Semantic Search</h3>
              </div>
              <p className="text-sm text-gray-400">
                Find memories by meaning, not just keywords - discover
                connections you didn&apos;t know existed.
              </p>
            </div>

            <div className="bg-gray-700/30 rounded-lg p-4">
              <div className="flex items-center gap-3 mb-2">
                <span className="text-lg">💡</span>
                <h3 className="font-medium text-white">Smart Suggestions</h3>
              </div>
              <p className="text-sm text-gray-400">
                Get smart search suggestions and related terms to explore your
                knowledge base.
              </p>
            </div>

            <div className="bg-gray-700/30 rounded-lg p-4">
              <div className="flex items-center gap-3 mb-2">
                <span className="text-lg">🎯</span>
                <h3 className="font-medium text-white">Relevance Scoring</h3>
              </div>
              <p className="text-sm text-gray-400">
                Results ranked by intelligent relevance scoring for the most
                accurate matches.
              </p>
            </div>
          </div>

          {/* Premium Badge */}
          <div className="inline-flex items-center gap-2 bg-gradient-to-r from-orange-500/20 to-purple-600/20 border border-orange-500/30 rounded-full px-4 py-2 mb-6">
            <span className="text-lg">🔒</span>
            <span className="text-white font-medium">Premium Feature</span>
          </div>

          {/* Call to Action */}
          <div className="space-y-4">
            <p className="text-gray-300">
              For now, use the Dashboard to explore ideas and automatically
              create searchable memories.
            </p>

            <Link href="/dashboard">
              <Button className="px-8 py-3">Back to Dashboard</Button>
            </Link>
          </div>
        </div>

        {/* Additional Info */}
        <p className="text-gray-500 text-sm mt-6">
          Want to be notified when search becomes available? Your memories are
          being saved automatically for future search functionality.
        </p>
      </div>
    </div>
  );
};

export default SearchPage;
