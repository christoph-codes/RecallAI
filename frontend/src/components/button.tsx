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
      "bg-white text-primary border border-white hover:border-white/40 rounded px-4 py-2 cursor-pointer hover:bg-white/40 hover:text-white focus:ring-white/40",
    secondary:
      "bg-transparent border border-white text-white hover:bg-primary-dark hover:text-white focus:ring-white",
    ghost: "bg-transparent text-white hover:bg-white/10 focus:ring-white",
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
