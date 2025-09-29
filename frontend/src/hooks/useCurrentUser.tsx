"use client";

import { useState, useEffect } from "react";
import { User, Session } from "@supabase/supabase-js";
import { supabase } from "@/configs/supabase";

interface UseCurrentUserReturn {
  user: User | null;
  session: Session | null;
  loading: boolean;
  error: string | null;
  signOut: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const useCurrentUser = (): UseCurrentUserReturn => {
  const [user, setUser] = useState<User | null>(null);
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshUser = async () => {
    try {
      setLoading(true);
      setError(null);

      const {
        data: { session },
        error,
      } = await supabase.auth.getSession();

      if (error) {
        console.error("Error fetching session:", error);
        setError(error.message);
        setUser(null);
        setSession(null);
      } else {
        setUser(session?.user ?? null);
        setSession(session);
      }
    } catch (err) {
      console.error("Unexpected error:", err);
      setError("An unexpected error occurred");
      setUser(null);
      setSession(null);
    } finally {
      setLoading(false);
    }
  };

  const signOut = async () => {
    try {
      setError(null);
      const { error } = await supabase.auth.signOut();

      if (error) {
        console.error("Error signing out:", error);
        setError(error.message);
      } else {
        setUser(null);
        setSession(null);
      }
    } catch (err) {
      console.error("Unexpected error during sign out:", err);
      setError("An unexpected error occurred during sign out");
    }
  };

  useEffect(() => {
    // Get initial session
    refreshUser();

    // Listen for auth state changes
    const {
      data: { subscription },
    } = supabase.auth.onAuthStateChange(async (event, session) => {
      setUser(session?.user ?? null);
      setSession(session);
      setLoading(false);
      setError(null);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, []);

  return {
    user,
    session,
    loading,
    error,
    signOut,
    refreshUser,
  };
};

export default useCurrentUser;
