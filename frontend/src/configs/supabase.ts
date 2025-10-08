"use client";

import { createClient } from "@supabase/supabase-js";

const buildTimeFallbackUrl = "https://placeholder.supabase.co";
const buildTimeFallbackKey = "public-anon-key";

const supabaseUrl =
  process.env.NEXT_PUBLIC_SUPABASE_URL ??
  (typeof window === "undefined" ? buildTimeFallbackUrl : undefined);

const supabaseKey =
  process.env.NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY ??
  (typeof window === "undefined" ? buildTimeFallbackKey : undefined);

if (!supabaseUrl || !supabaseKey) {
  throw new Error(
    "Supabase environment variables NEXT_PUBLIC_SUPABASE_URL and NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY must be set."
  );
}

export const supabase = createClient(supabaseUrl, supabaseKey);
