import { useState, useCallback } from "react";

export type UseToggleReturn = {
  isOpen: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
};

/**
 * Custom hook for managing toggle state (open/close/toggle)
 * Useful for modals, popovers, dropdowns, sidenavs, etc.
 *
 * @param initialState - Initial state value (default: false)
 * @returns Object with isOpen state and control functions
 */
export const useToggle = (initialState: boolean = false): UseToggleReturn => {
  const [isOpen, setIsOpen] = useState(initialState);

  const open = useCallback(() => {
    setIsOpen(true);
  }, []);

  const close = useCallback(() => {
    setIsOpen(false);
  }, []);

  const toggle = useCallback(() => {
    setIsOpen((prev) => !prev);
  }, []);

  return {
    isOpen,
    open,
    close,
    toggle,
  };
};
