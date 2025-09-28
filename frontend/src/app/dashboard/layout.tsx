"use client";
import SharedLayout from "@/components/SharedLayout";
import { ReactNode } from "react";
import { useHealthCheck } from "@/queries";

const DashboardLayout = ({ children }: { children: ReactNode }) => {
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

  return <SharedLayout>{children}</SharedLayout>;
};

export default DashboardLayout;
