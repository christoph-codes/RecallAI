import SharedLayout from "@/components/SharedLayout";
import { ReactNode } from "react";

const SearchLayout = ({ children }: { children: ReactNode }) => {
  return <SharedLayout>{children}</SharedLayout>;
};

export default SearchLayout;
