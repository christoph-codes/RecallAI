import { ChangeEvent } from "react";

export type InputProps = {
  value: string;
  onChange: (e: ChangeEvent<HTMLInputElement>) => void;
  type?: string;
  placeholder?: string;
  label?: string;
};

const Input = ({
  value,
  onChange,
  type = "text",
  placeholder = "Type something...",
  label,
}: InputProps) => {
  return (
    <div className="flex flex-col gap-1 flex-1">
      {label && <label className="font-bold text-gray-200">{label}</label>}
      <input
        className="border-2 border-gray-600 bg-gray-800 text-gray-200 placeholder-gray-400 rounded-md p-2 w-full focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition"
        value={value}
        onChange={onChange}
        type={type}
        placeholder={placeholder}
      />
    </div>
  );
};
export default Input;
