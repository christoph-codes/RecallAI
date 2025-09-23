"use client";
import Button from "@/components/button";
import Input from "@/components/input";
import { supabase } from "@/configs/supabase";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useState } from "react";

const Create = () => {
  const router = useRouter();
  //   const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const handleSignUp = async () => {
    if (password !== confirmPassword) {
      console.error("Passwords do not match");
      return;
    }
    const { data, error } = await supabase.auth.signUp({
      email,
      password,
    });
    if (error) {
      console.error("Sign up failed:", error);
    } else {
      console.log("Sign up successful:", data);
      router.push("/dashboard");
    }
  };
  return (
    <div className="flex flex-col gap-4 w-full max-w-sm items-center">
      <Image
        className="text-white mb-4"
        src="/recall_white.svg"
        alt="Login to Recall"
        width={280}
        height={38}
        priority
      />
      <p className="max-w-prose text-balance text-xl font-bold text-white/90 text-center">
        Sign up today!
      </p>
      <div className="flex flex-col gap-4 w-full items-stretch">
        {/* <Input
          label="Full Name"
          placeholder="John Doe"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
        /> */}
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
        <Button onClick={handleSignUp}>Sign up</Button>
        <Button variant="ghost" onClick={() => router.push("/auth/login")}>
          Already have an account?
        </Button>
      </div>
    </div>
  );
};

export default Create;
