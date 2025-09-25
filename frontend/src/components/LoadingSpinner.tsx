"use client";

import React from "react";

interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg" | "xl";
  message?: string;
  variant?: "default" | "minimal" | "branded";
  className?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = "md",
  message,
  variant = "default",
  className = "",
}) => {
  const sizeConfig = {
    sm: {
      spinner: "w-4 h-4",
      logo: "w-3 h-3",
      container: "gap-1",
      text: "text-xs",
      dots: "w-1 h-1",
    },
    md: {
      spinner: "w-6 h-6",
      logo: "w-4 h-4",
      container: "gap-2",
      text: "text-sm",
      dots: "w-1.5 h-1.5",
    },
    lg: {
      spinner: "w-8 h-8",
      logo: "w-6 h-6",
      container: "gap-3",
      text: "text-base",
      dots: "w-2 h-2",
    },
    xl: {
      spinner: "w-12 h-12",
      logo: "w-8 h-8",
      container: "gap-4",
      text: "text-lg",
      dots: "w-3 h-3",
    },
  };

  const config = sizeConfig[size];

  if (variant === "minimal") {
    return (
      <div className={`flex items-center justify-center ${className}`}>
        <div className={`${config.spinner} animate-spin`}>
          <div className="border-2 border-gray-600 border-t-orange-500 rounded-full w-full h-full"></div>
        </div>
      </div>
    );
  }

  if (variant === "branded") {
    return (
      <div
        className={`flex flex-col items-center justify-center ${config.container} ${className}`}
      >
        {/* Animated Brain with Pulsing Glow */}
        <div className="relative">
          <div className={`${config.spinner} relative animate-pulse`}>
            {/* Outer glow ring */}
            <div className="absolute inset-0 bg-gradient-to-r from-orange-500/30 to-purple-600/30 rounded-full animate-ping"></div>
            {/* Main brain container */}
            <div className="relative bg-gradient-to-r from-orange-500 to-orange-600 rounded-full flex items-center justify-center w-full h-full">
              <span className={`${config.text} animate-bounce`}>🧠</span>
            </div>
          </div>
          {/* Orbiting dots */}
          <div className="absolute inset-0 animate-spin">
            <div
              className={`absolute -top-1 left-1/2 transform -translate-x-1/2 ${config.dots} bg-orange-400 rounded-full`}
            ></div>
            <div
              className={`absolute top-1/2 -right-1 transform -translate-y-1/2 ${config.dots} bg-purple-400 rounded-full`}
            ></div>
            <div
              className={`absolute -bottom-1 left-1/2 transform -translate-x-1/2 ${config.dots} bg-orange-400 rounded-full`}
            ></div>
            <div
              className={`absolute top-1/2 -left-1 transform -translate-y-1/2 ${config.dots} bg-purple-400 rounded-full`}
            ></div>
          </div>
        </div>

        {message && (
          <p
            className={`text-gray-400 ${config.text} text-center animate-pulse`}
          >
            {message}
          </p>
        )}
      </div>
    );
  }

  // Default variant
  return (
    <div
      className={`flex items-center justify-center ${config.container} ${className}`}
    >
      {/* Spinning gradient ring */}
      <div className={`relative ${config.spinner}`}>
        <div className="absolute inset-0 bg-gradient-to-r from-orange-500 via-purple-500 to-orange-500 rounded-full animate-spin">
          <div className="absolute inset-1 bg-gray-900 rounded-full"></div>
        </div>
        {/* Center logo */}
        <div className="absolute inset-0 flex items-center justify-center">
          <div
            className={`${config.logo} bg-gradient-to-r from-orange-400 to-orange-600 rounded-full flex items-center justify-center animate-pulse`}
          >
            <span className="text-white text-xs">R</span>
          </div>
        </div>
      </div>

      {message && (
        <div className="flex flex-col items-center">
          <p className={`text-gray-300 ${config.text} font-medium`}>
            {message}
          </p>
          {/* Animated dots */}
          <div className="flex items-center gap-1 mt-1">
            <div
              className={`${config.dots} bg-orange-500 rounded-full animate-bounce`}
              style={{ animationDelay: "0ms" }}
            ></div>
            <div
              className={`${config.dots} bg-orange-500 rounded-full animate-bounce`}
              style={{ animationDelay: "150ms" }}
            ></div>
            <div
              className={`${config.dots} bg-orange-500 rounded-full animate-bounce`}
              style={{ animationDelay: "300ms" }}
            ></div>
          </div>
        </div>
      )}
    </div>
  );
};

export default LoadingSpinner;
