import { PropsWithChildren } from "react";

const Layout = ({ children }: PropsWithChildren) => {
  return (
    <main className="flex flex-col gap-8 items-center justify-center sm:items-start p-8 pb-20 sm:p-20 min-h-screen">
      <div className="flex flex-col gap-4 text-center sm:text-left items-center justify-center w-full">
        {children}
      </div>
    </main>
  );
};

export default Layout;
