"use client";

import { useUser } from "@/contexts/UserContext";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import Login from "./auth/login/login";

export default function Page() {
  const { user, loading } = useUser();
  const router = useRouter();

  useEffect(() => {
    if (!loading && user) {
      router.push("/dashboard");
    }
  }, [user, loading, router]);

  // Show loading while checking auth state
  if (loading) {
    return (
      <main className="flex flex-col gap-8 items-center justify-center sm:items-start p-8 pb-20 sm:p-20 min-h-screen">
        <div className="text-center">
          <div className="text-xl text-white">Loading...</div>
        </div>
      </main>
    );
  }

  // Don't render login if user is authenticated (will redirect)
  if (user) {
    return null;
  }

  return (
    <main className="flex flex-col gap-8 items-center justify-center sm:items-start p-8 pb-20 sm:p-20 min-h-screen">
      <div className="flex flex-col gap-4 text-center sm:text-left items-center justify-center w-full">
        <Login />
      </div>
    </main>
  );
}
