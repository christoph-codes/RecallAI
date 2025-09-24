"use client";
import Button from "@/components/button";
import Input from "@/components/input";
import { supabase } from "@/configs/supabase";
import { useUser } from "@/contexts/UserContext";
import AlreadyAuthenticatedMessage from "@/components/AlreadyAuthenticatedMessage";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useState } from "react";

const Login = () => {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Get user state but don't auto-redirect
  const { user, loading } = useUser();

  // Show loading while checking auth state
  if (loading) {
    return (
      <div className="flex flex-col gap-4 w-full max-w-sm items-center">
        <div className="text-gray-200 text-center">Loading...</div>
      </div>
    );
  }

  // Show already authenticated message if user is logged in
  if (user) {
    return <AlreadyAuthenticatedMessage />;
  }

  const handleLogin = async () => {
    if (isLoggingIn) return; // Prevent double submission

    setIsLoggingIn(true);
    setError("");

    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password,
    });

    if (error) {
      setError(error.message);
      setIsLoggingIn(false);
    } else {
      console.log("Login successful:", data);
      // Don't manually redirect here - the auth state change will handle it
    }
  };
  return (
    <div className="flex flex-col gap-4 w-full max-w-sm items-center">
      <Image
        className="text-orange-500 mb-4"
        src="/recall_orange.svg"
        alt="Login to Recall"
        width={280}
        height={38}
        priority
      />
      <p className="max-w-prose text-balance text-xl font-bold text-gray-200 text-center">
        Save a thought once, <span className="text-orange-500">recall</span> it
        anytime.
      </p>
      <p className="max-w-prose  text-base text-gray-300 text-center">
        Think less about remembering, and more about what matters today.
      </p>
      <div className="flex flex-col gap-4 w-full items-stretch">
        <Input
          label="Email"
          placeholder="john@doe.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <Input
          label="Password"
          placeholder="********"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        {error && <p className="text-red-400 text-sm font-bold">{error}</p>}
        <Button onClick={handleLogin}>
          {isLoggingIn ? "Logging in..." : "Login"}
        </Button>
        <Button
          variant="ghost"
          onClick={() => !isLoggingIn && router.push("/auth/create")}
        >
          Need an account?
        </Button>
      </div>
    </div>
  );
};

export default Login;