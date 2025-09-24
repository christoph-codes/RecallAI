"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useUser } from "@/contexts/UserContext";

interface UseAuthGuardOptions {
  redirectTo?: string;
  requireAuth?: boolean;
}

export const useAuthGuard = ({
  redirectTo = "/",
  requireAuth = true,
}: UseAuthGuardOptions = {}) => {
  const { user, loading } = useUser();
  const router = useRouter();
  const [hasRedirected, setHasRedirected] = useState(false);

  useEffect(() => {
    // Only redirect once auth state is determined and we haven't redirected yet
    if (!loading && !hasRedirected) {
      if (requireAuth && !user) {
        // User should be authenticated but isn't - redirect to login
        setHasRedirected(true);
        router.push(redirectTo);
      } else if (!requireAuth && user) {
        // User shouldn't be on auth pages when logged in - redirect to dashboard
        setHasRedirected(true);
        router.push("/dashboard");
      }
    }
  }, [user, loading, requireAuth, redirectTo, router, hasRedirected]);

  // Reset redirect flag when user state changes
  useEffect(() => {
    if (!loading) {
      setHasRedirected(false);
    }
  }, [user, loading]);

  return { user, loading };
};
