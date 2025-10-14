import { useEffect, useCallback } from 'react';

export interface HotkeyConfig {
  key: string;
  ctrlKey?: boolean;
  shiftKey?: boolean;
  altKey?: boolean;
  metaKey?: boolean;
  callback: () => void;
  description?: string;
  enabled?: boolean;
}

export interface UseHotkeysOptions {
  enabled?: boolean;
  preventDefault?: boolean;
  stopPropagation?: boolean;
}

/**
 * Custom hook for handling keyboard shortcuts
 * 
 * @param hotkeys Array of hotkey configurations
 * @param options Configuration options
 */
export const useHotkeys = (
  hotkeys: HotkeyConfig[],
  options: UseHotkeysOptions = {}
) => {
  const {
    enabled = true,
    preventDefault = true,
    stopPropagation = false
  } = options;

  const handleKeyDown = useCallback((event: KeyboardEvent) => {
    if (!enabled) return;

    // Find matching hotkey
    const matchingHotkey = hotkeys.find(hotkey => {
      if (!hotkey.enabled) return false;
      
      return event.key.toLowerCase() === hotkey.key.toLowerCase() &&
             !!event.ctrlKey === !!hotkey.ctrlKey &&
             !!event.shiftKey === !!hotkey.shiftKey &&
             !!event.altKey === !!hotkey.altKey &&
             !!event.metaKey === !!hotkey.metaKey;
    });

    if (matchingHotkey) {
      if (preventDefault) {
        event.preventDefault();
      }
      
      if (stopPropagation) {
        event.stopPropagation();
      }

      matchingHotkey.callback();
    }
  }, [hotkeys, enabled, preventDefault, stopPropagation]);

  useEffect(() => {
    if (enabled) {
      document.addEventListener('keydown', handleKeyDown);
      return () => document.removeEventListener('keydown', handleKeyDown);
    }
  }, [handleKeyDown, enabled]);
};

/**
 * Helper function to create common hotkey configurations
 */
export const createHotkey = (
  key: string,
  callback: () => void,
  options: Partial<HotkeyConfig> = {}
): HotkeyConfig => ({
  key,
  callback,
  enabled: true,
  ...options
});

/**
 * Common hotkey combinations
 */
export const HotkeyKeys = {
  // Navigation
  NEXT: 'ArrowRight',
  PREV: 'ArrowLeft',
  UP: 'ArrowUp',
  DOWN: 'ArrowDown',
  
  // Page navigation
  NEXT_PAGE: 'PageDown',
  PREV_PAGE: 'PageUp',
  
  // Collection navigation
  NEXT_COLLECTION: 'ArrowRight',
  PREV_COLLECTION: 'ArrowLeft',
  
  // Modifier combinations
  CTRL_NEXT: 'ArrowRight',
  CTRL_PREV: 'ArrowLeft',
  SHIFT_NEXT: 'ArrowRight',
  SHIFT_PREV: 'ArrowLeft',
  
  // Other
  ESCAPE: 'Escape',
  ENTER: 'Enter',
  SPACE: ' ',
} as const;

/**
 * Predefined hotkey configurations for common actions
 */
export const CommonHotkeys = {
  // Page navigation
  nextPage: (callback: () => void) => createHotkey(HotkeyKeys.NEXT_PAGE, callback, {
    description: 'Next page'
  }),
  prevPage: (callback: () => void) => createHotkey(HotkeyKeys.PREV_PAGE, callback, {
    description: 'Previous page'
  }),
  
  // Collection navigation with Ctrl
  nextCollection: (callback: () => void) => createHotkey(HotkeyKeys.CTRL_NEXT, callback, {
    ctrlKey: true,
    description: 'Next collection'
  }),
  prevCollection: (callback: () => void) => createHotkey(HotkeyKeys.CTRL_PREV, callback, {
    ctrlKey: true,
    description: 'Previous collection'
  }),
  
  // Collection navigation with Shift
  nextCollectionShift: (callback: () => void) => createHotkey(HotkeyKeys.SHIFT_NEXT, callback, {
    shiftKey: true,
    description: 'Next collection'
  }),
  prevCollectionShift: (callback: () => void) => createHotkey(HotkeyKeys.SHIFT_PREV, callback, {
    shiftKey: true,
    description: 'Previous collection'
  }),
  
  // Image navigation
  nextImage: (callback: () => void) => createHotkey(HotkeyKeys.NEXT, callback, {
    description: 'Next image'
  }),
  prevImage: (callback: () => void) => createHotkey(HotkeyKeys.PREV, callback, {
    description: 'Previous image'
  }),
  
  // Collection navigation with Up/Down arrows
  nextCollectionUpDown: (callback: () => void) => createHotkey(HotkeyKeys.DOWN, callback, {
    description: 'Next collection'
  }),
  prevCollectionUpDown: (callback: () => void) => createHotkey(HotkeyKeys.UP, callback, {
    description: 'Previous collection'
  }),
} as const;
