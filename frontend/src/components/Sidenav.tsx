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

const Sidenav = ({ isOpen, onClose }: SidenavProps) => {
  const { user, signOut } = useUser();
  const pathname = usePathname();

  const handleSignOut = () => {
    signOut();
    onClose();
  };

  const isActive = (path: string) => pathname === path;

  const navigationItems = [
    {
      name: "Dashboard",
      href: "/dashboard",
      icon: "🏠",
      description: "Your workspace for ideas and insights",
      enabled: true,
    },
    {
      name: "Search",
      href: "/search",
      icon: "🔍",
      description: "Enhanced search (Coming Soon)",
      enabled: false,
    },
  ];

  return (
    <>
      {/* Overlay */}
      {isOpen && (
        <button
          className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 transition-opacity cursor-default"
          onClick={onClose}
          onKeyDown={(e) => {
            if (e.key === "Escape") onClose();
          }}
          aria-label="Close menu overlay"
        />
      )}

      {/* Slide-out Sidenav */}
      <div
        className={`fixed top-0 right-0 h-full w-80 max-w-[80vw] bg-gray-800 shadow-2xl transform transition-transform duration-300 ease-in-out z-50 flex flex-col ${
          isOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {/* Sidenav Header */}
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

        {/* Navigation Section */}
        <div className="px-4 pb-4">
          <div className="space-y-2">
            {navigationItems.map((item) => {
              if (item.enabled) {
                return (
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
                    <span className="text-xl">{item.icon}</span>
                    <div className="flex-1">
                      <div className="font-medium">{item.name}</div>
                      <div className="text-xs opacity-75">
                        {item.description}
                      </div>
                    </div>
                    {isActive(item.href) && (
                      <div className="w-2 h-2 bg-orange-400 rounded-full animate-pulse" />
                    )}
                  </Link>
                );
              } else {
                return (
                  <div
                    key={item.href}
                    className="flex items-center gap-3 p-3 rounded-lg opacity-50 cursor-not-allowed relative group"
                  >
                    <span className="text-xl grayscale">{item.icon}</span>
                    <div className="flex-1">
                      <div className="font-medium text-gray-400 flex items-center gap-2">
                        {item.name}
                        <span className="text-xs">🔒</span>
                      </div>
                      <div className="text-xs text-gray-500">
                        {item.description}
                      </div>
                    </div>
                    <div className="text-xs bg-gray-600/50 text-gray-400 px-2 py-1 rounded-full border border-gray-600/30">
                      Premium
                    </div>

                    {/* Tooltip */}
                    <div className="absolute left-0 top-full mt-2 w-64 bg-gray-800 border border-gray-600 rounded-lg p-3 text-sm text-gray-300 opacity-0 group-hover:opacity-100 transition-opacity duration-200 z-10 shadow-lg">
                      <div className="font-medium text-white mb-1">
                        Search Feature
                      </div>
                      <p className="text-xs">
                        AI-enhanced search will be available in a future update.
                        Stay tuned!
                      </p>
                    </div>
                  </div>
                );
              }
            })}
          </div>
        </div>

        {/* User Info Section */}
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

            {/* Sign Out Button */}
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
