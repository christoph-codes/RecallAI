"use client";
import Button from "@/components/button";
import Input from "@/components/input";
import { supabase } from "@/configs/supabase";
import { useUser } from "@/contexts/UserContext";
import AlreadyAuthenticatedMessage from "@/components/AlreadyAuthenticatedMessage";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useState } from "react";

const Create = () => {
  const router = useRouter();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [isCreatingAccount, setIsCreatingAccount] = useState(false);

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

  const handleSignUp = async () => {
    if (isCreatingAccount) return; // Prevent double submission

    if (password !== confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    setIsCreatingAccount(true);
    setError("");

    const { data, error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: {
          full_name: fullName,
        },
      },
    });

    if (error) {
      setError(error.message);
      setIsCreatingAccount(false);
    } else {
      console.log("Sign up successful:", data);
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
        Create your Recall account
      </p>
      <p className="max-w-prose  text-base text-gray-300 text-center">
        Remember the things that matter most.
      </p>
      <div className="flex flex-col gap-4 w-full items-stretch">
        <Input
          label="Full Name"
          placeholder="John Doe"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
        />
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
        <Input
          label="Confirm Password"
          placeholder="********"
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
        />
        {error && <p className="text-red-400 text-sm font-bold">{error}</p>}
        <Button onClick={handleSignUp}>
          {isCreatingAccount ? "Creating account..." : "Sign up"}
        </Button>
        <Button
          variant="ghost"
          onClick={() => !isCreatingAccount && router.push("/")}
        >
          Already have an account?
        </Button>
      </div>
    </div>
  );
};

export default Create;
