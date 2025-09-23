"use client";
import Button from "@/components/button";
import Input from "@/components/input";
import { supabase } from "@/configs/supabase";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useState } from "react";

const Login = () => {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const handleLogin = async () => {
    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password,
    });
    if (error) {
      setError(error.message);
    } else {
      console.log("Login successful:", data);
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
        Save a thought once. Recall it anytime.
      </p>
      <p className="max-w-prose  text-base text-white/90 text-center">
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
        {error && <p className="text-black text-sm font-bold">{error}</p>}
        <Button onClick={handleLogin}>Login</Button>
        <Button variant="ghost" onClick={() => router.push("/auth/create")}>
          Need an account?
        </Button>
      </div>
    </div>
  );
};

export default Login;
