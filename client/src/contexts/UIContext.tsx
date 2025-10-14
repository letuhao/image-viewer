import { createContext, useContext, useState, ReactNode } from 'react';

interface UIContextType {
  hideDevTools: boolean;
  setHideDevTools: (hide: boolean) => void;
}

const UIContext = createContext<UIContextType | undefined>(undefined);

export function UIProvider({ children }: { children: ReactNode }) {
  const [hideDevTools, setHideDevTools] = useState(false);

  return (
    <UIContext.Provider value={{ hideDevTools, setHideDevTools }}>
      {children}
    </UIContext.Provider>
  );
}

export function useUI() {
  const context = useContext(UIContext);
  if (context === undefined) {
    throw new Error('useUI must be used within a UIProvider');
  }
  return context;
}
