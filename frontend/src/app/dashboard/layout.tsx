"use client";
import Button from "@/components/button";
import { useUser } from "@/contexts/UserContext";
import Image from "next/image";
import { ReactNode } from "react";

const DashboardLayout = ({ children }: { children: ReactNode }) => {
  const { user, signOut } = useUser();

  return (
    <div className="min-h-screen flex flex-col bg-gray-900">
      <div className="max-w-4xl mx-auto w-full flex flex-col h-screen">
        {/* Header with Logo and User Actions */}
        <div className="flex justify-between items-center p-4 pb-3 flex-shrink-0 border-b border-gray-700/50">
          <div className="flex items-center">
            <Image
              src="/recall_white.svg"
              alt="Recall AI"
              width={120}
              height={20}
              priority
              className="h-6 w-auto"
            />
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-400">Hello, {user?.email}</span>
            <Button variant="secondary" onClick={signOut}>
              Sign Out
            </Button>
          </div>
        </div>

        {/* Main Content Area */}
        <div className="bg-gray-900 text-white flex-1 overflow-auto">
          {children}
        </div>
      </div>
    </div>
  );
};

export default DashboardLayout;
