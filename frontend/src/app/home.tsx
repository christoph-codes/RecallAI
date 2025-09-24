"use client";
import Button from "@/components/button";
import Image from "next/image";
import { useRouter } from "next/navigation";

const Home = () => {
  const router = useRouter();
  return (
    <main className="flex flex-col gap-8 items-center justify-center sm:items-start p-8 pb-20 sm:p-20 min-h-screen">
      <div className="flex flex-col gap-4 text-center sm:text-left items-center justify-center w-full">
        <Image
          className="text-white mb-4"
          src="/recall_white.svg"
          alt="Recall logo"
          width={280}
          height={38}
          priority
        />
        <p className="max-w-prose text-balance text-xl font-bold text-white/90 text-center">
          Save a thought once. Recall it anytime.
        </p>
        <p className="max-w-prose text-balance text-base text-white/90 text-center">
          Think less about remembering, and more about what matters today.
        </p>
        <div className="flex items-center justify-center gap-3">
          <Button variant="primary" onClick={() => router.push("/auth/login")}>
            Login
          </Button>
          <Button
            variant="secondary"
            onClick={() => router.push("/auth/create")}
          >
            Get Started
          </Button>
        </div>
        <p className="text-xs text-center text-white/70">
          No fluff. Your data stays yours.
        </p>
      </div>
    </main>
  );
};

export default Home;
