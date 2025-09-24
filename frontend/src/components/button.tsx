import { ReactNode } from "react";

export type ButtonProps = {
  variant?: "primary" | "secondary" | "danger" | "ghost";
  children: ReactNode;
  onClick?: VoidFunction;
  className?: string;
};

const Button = ({
  variant = "primary",
  children,
  className,
  onClick,
}: Readonly<ButtonProps>) => {
  const baseStyles =
    "px-4 py-2 rounded-md font-bold focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors cursor-pointer min-w-[120px] text-center";
  const variants = {
    primary:
      "bg-orange-500 text-white border border-orange-500 hover:bg-orange-600 hover:border-orange-600 rounded px-4 py-2 cursor-pointer focus:ring-orange-400",
    secondary:
      "bg-transparent border border-gray-600 text-gray-200 hover:bg-gray-700 hover:text-white focus:ring-gray-500",
    ghost: "bg-transparent text-gray-300 hover:bg-gray-800 focus:ring-gray-600",
    danger: "bg-red-600 text-white hover:bg-red-700 focus:ring-red-500",
  };

  return (
    <button
      onClick={onClick}
      className={`${baseStyles} ${variants[variant]} ${className}`}
    >
      {children}
    </button>
  );
};

export default Button;
