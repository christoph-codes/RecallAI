"use client";

import { useUser } from "@/contexts/UserContext";
import Button from "@/components/button";
import { useRouter } from "next/navigation";
import Image from "next/image";

const AlreadyAuthenticatedMessage = () => {
  const { user, signOut } = useUser();
  const router = useRouter();

  const handleGoToDashboard = () => {
    router.push("/dashboard");
  };

  const handleSignOut = async () => {
    await signOut();
    // User will be redirected to login after sign out
  };

  return (
    <div className="flex flex-col gap-6 w-full max-w-sm items-center text-center">
      <Image
        className="text-white mb-4"
        src="/recall_white.svg"
        alt="Recall AI"
        width={280}
        height={38}
        priority
      />
      <div className="bg-white/10 backdrop-blur-sm rounded-lg p-6 border border-white/20">
        <h2 className="text-xl font-bold text-white mb-3">
          You&apos;re Already Signed In! 👋
        </h2>
        <p className="text-white/80 mb-4">
          Welcome back, <span className="font-medium">{user?.email}</span>
        </p>
        <p className="text-white/70 text-sm mb-6">
          You&apos;re already authenticated and ready to go. Would you like to
          continue to your dashboard or sign in with a different account?
        </p>
        <div className="flex flex-col gap-3">
          <Button onClick={handleGoToDashboard}>Go to Dashboard</Button>
          <Button variant="ghost" onClick={handleSignOut}>
            Sign in with different account
          </Button>
        </div>
      </div>
    </div>
  );
};

export default AlreadyAuthenticatedMessage;
