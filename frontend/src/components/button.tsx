import { ReactNode } from "react";

export type ButtonProps = {
  variant?: "primary" | "secondary" | "danger" | "ghost" | "glass";
  children: ReactNode;
  onClick?: VoidFunction;
  className?: string;
  type?: "button" | "submit" | "reset";
  disabled?: boolean;
};

const Button = ({
  variant = "primary",
  children,
  className,
  onClick,
  type,
  disabled = false,
}: Readonly<ButtonProps>) => {
  const baseStyles =
    "px-4 py-2 rounded-md font-bold focus:outline-none focus:ring-1 focus:ring-offset-1 transition-colors cursor-pointer min-w-[120px] text-center disabled:opacity-50 disabled:cursor-auto disabled:hover:bg-current";
  const variants = {
    primary:
      "bg-orange-500 text-white border border-orange-500 hover:bg-orange-600 hover:border-orange-600 rounded px-4 py-2 cursor-pointer focus:ring-orange-400 disabled:hover:bg-orange-500 disabled:hover:border-orange-500",
    secondary:
      "bg-transparent border border-gray-600 text-gray-200 hover:bg-gray-700 hover:text-white focus:ring-gray-500 disabled:hover:bg-transparent disabled:hover:text-gray-200",
    ghost:
      "bg-transparent text-gray-300 hover:bg-gray-800 focus:ring-gray-600 disabled:hover:bg-transparent",
    danger:
      "bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 disabled:hover:bg-red-600",
    glass:
      "bg-gray-700/50 backdrop-blur-sm rounded-xl hover:border-primary-light border border-gray-600/30 transition-all duration-200 focus:outline-none focus:ring-1 focus:ring-orange-500/50 disabled:opacity-50 hover:bg-primary hover:border-orange-500 disabled:hover:border-transparent disabled:hover:bg-gray-700/50",
  };

  return (
    <button
      onClick={onClick}
      className={`${baseStyles} ${variants[variant]} ${className}`}
      type={type}
      disabled={disabled}
    >
      {children}
    </button>
  );
};

export default Button;
