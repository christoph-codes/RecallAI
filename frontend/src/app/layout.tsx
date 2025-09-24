import type { Metadata } from "next";
import "./globals.css";
import ClientProviders from "@/components/ClientProviders";

export const metadata: Metadata = {
  title: "Recall AI",
  description: "Remember the things that matter most.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased bg-primary font-sans">
        <ClientProviders>{children}</ClientProviders>
      </body>
    </html>
  );
}
