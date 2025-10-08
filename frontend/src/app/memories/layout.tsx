import SharedLayout from "@/components/SharedLayout";
import { ReactNode } from "react";

const MemoriesLayout = ({ children }: { children: ReactNode }) => {
  return <SharedLayout>{children}</SharedLayout>;
};

export default MemoriesLayout;
