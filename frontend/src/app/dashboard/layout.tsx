"use client";
import Image from "next/image";
import { ReactNode } from "react";
import Sidenav from "@/components/Sidenav";
import { useToggle } from "@/hooks/useToggle";
import { useHealthCheck } from "@/queries";

const DashboardLayout = ({ children }: { children: ReactNode }) => {
  const sidenav = useToggle(false);

  // Health check hook - runs automatically on mount
  const { data: healthData, error: healthError } = useHealthCheck({
    autoCheck: true,
    interval: 0, // No automatic re-checking for now
  });

  // Log health check results for development (remove this later)
  if (healthData) {
    console.log("Backend health check:", healthData);
  }
  if (healthError) {
    console.error("Backend health check failed:", healthError.message);
  }

  return (
    <div className="min-h-screen flex flex-col bg-gray-900">
      <div className="max-w-4xl gap-2 mx-auto w-full flex flex-col h-screen">
        {/* Header with Logo and Hamburger Menu */}
        <div className="flex justify-between items-center p-4 pb-3 flex-shrink-0 border-b border-gray-700/50">
          <div className="flex items-center">
            <Image
              src="/recall_orange.svg"
              alt="Recall AI"
              width={120}
              height={20}
              priority
              className="h-6 w-auto"
            />
          </div>

          {/* Hamburger Menu Button */}
          <button
            onClick={sidenav.toggle}
            className="p-2 cursor-pointer text-gray-300 hover:text-white hover:bg-gray-800 rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-orange-500/50"
            aria-label="Open menu"
          >
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 6h16M4 12h16M4 18h16"
              />
            </svg>
          </button>
        </div>

        {/* Main Content Area */}
        <div className="bg-gray-900 text-white flex-1 overflow-auto">
          {children}
        </div>
      </div>

      {/* Sidenav Component */}
      <Sidenav isOpen={sidenav.isOpen} onClose={sidenav.close} />
    </div>
  );
};

export default DashboardLayout;
