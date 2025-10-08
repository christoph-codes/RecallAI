"use client";
import Button from "@/components/button";
import { useUser } from "@/contexts/UserContext";
import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";

interface SidenavProps {
  isOpen: boolean;
  onClose: () => void;
}

const navigationItems = [
  {
    name: "Dashboard",
    href: "/dashboard",
    badge: "DB",
    description: "Your workspace for ideas and insights",
  },
  {
    name: "Memories",
    href: "/memories",
    badge: "MB",
    description: "Browse and manage saved memories",
  },
  {
    name: "Search",
    href: "/search",
    badge: "SR",
    description: "Semantic search across your knowledge base",
  },
];

const Sidenav = ({ isOpen, onClose }: SidenavProps) => {
  const { user, signOut } = useUser();
  const pathname = usePathname();

  const handleSignOut = () => {
    signOut();
    onClose();
  };

  const isActive = (path: string) => pathname === path;

  return (
    <>
      {isOpen && (
        <button
          className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 transition-opacity cursor-default"
          onClick={onClose}
          onKeyDown={(event) => {
            if (event.key === "Escape") onClose();
          }}
          aria-label="Close menu overlay"
        />
      )}

      <div
        className={`fixed top-0 right-0 h-full w-80 max-w-[80vw] bg-gray-800 shadow-2xl transform transition-transform duration-300 ease-in-out z-50 flex flex-col ${
          isOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        <div className="flex justify-between items-center p-4 border-b border-gray-700">
          <Image
            src="/recall_orange.svg"
            alt="Recall AI"
            width={100}
            height={16}
            priority
            className="h-5 w-auto"
          />
          <button
            onClick={onClose}
            className="p-2 text-gray-400 hover:text-white hover:bg-gray-700 rounded-lg transition-colors cursor-pointer"
            aria-label="Close menu"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        <div className="px-4 pb-4">
          <div className="space-y-2">
            {navigationItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                onClick={onClose}
                className={`flex items-center gap-3 p-3 rounded-lg transition-all duration-200 ${
                  isActive(item.href)
                    ? "bg-orange-500/20 text-orange-300 border border-orange-500/30"
                    : "text-gray-300 hover:text-white hover:bg-gray-700/50"
                }`}
              >
                <span className="text-xs font-semibold bg-gray-700/60 text-gray-200 px-2 py-1 rounded-md">
                  {item.badge}
                </span>
                <div className="flex-1">
                  <div className="font-medium">{item.name}</div>
                  <div className="text-xs opacity-75">{item.description}</div>
                </div>
                {isActive(item.href) && (
                  <div className="w-2 h-2 bg-orange-400 rounded-full animate-pulse" />
                )}
              </Link>
            ))}
          </div>
        </div>

        <div className="p-4 mt-auto">
          <div className="bg-gray-700/50 rounded-lg p-4 mb-4">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-10 h-10 bg-orange-500 rounded-full flex items-center justify-center">
                <span className="text-white font-semibold text-lg">
                  {user?.email?.charAt(0).toUpperCase()}
                </span>
              </div>
              <div>
                <p className="text-white font-medium">Account</p>
                <p className="text-gray-300 text-sm">{user?.email}</p>
              </div>
            </div>

            <Button
              variant="secondary"
              onClick={handleSignOut}
              className="w-full"
            >
              Sign Out
            </Button>
          </div>
        </div>
      </div>
    </>
  );
};

export default Sidenav;
