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
    <div className="flex flex-col gap-1">
      {label && <label className="font-bold">{label}</label>}
      <input
        className="border-2 border-gray-300 rounded-md p-2 w-full focus:outline-none focus:ring-2 focus:ring-primary-light focus:border-transparent transition"
        value={value}
        onChange={onChange}
        type={type}
        placeholder={placeholder}
      />
    </div>
  );
};
export default Input;
