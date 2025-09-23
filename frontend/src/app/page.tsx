import Image from "next/image";

export default function Home() {
  return (
    <div className="bg-primary font-sans flex items-center justify-center min-h-screen p-8 pb-20 gap-16 sm:p-20">
      <main className="flex flex-col gap-[32px] row-start-2 items-center sm:items-start">
        <Image
          className="text-white"
          src="/recall_white.svg"
          alt="Recall logo"
          width={180}
          height={38}
          priority
        />
      </main>
    </div>
  );
}
