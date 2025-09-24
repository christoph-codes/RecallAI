"use client";

import { UserProvider } from "@/contexts/UserContext";
import { ReactNode } from "react";

interface ClientProvidersProps {
  readonly children: ReactNode;
}

export default function ClientProviders({ children }: ClientProvidersProps) {
  return <UserProvider>{children}</UserProvider>;
}
