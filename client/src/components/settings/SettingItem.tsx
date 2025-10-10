import { ReactNode } from 'react';

interface SettingItemProps {
  label: string;
  description?: string;
  children: ReactNode;
  vertical?: boolean;
}

/**
 * Setting Item Component
 * 
 * Single setting row with label and control
 */
const SettingItem: React.FC<SettingItemProps> = ({
  label,
  description,
  children,
  vertical = false,
}) => {
  if (vertical) {
    return (
      <div className="space-y-2">
        <div>
          <label className="text-sm font-medium text-white block">{label}</label>
          {description && <p className="text-xs text-slate-400 mt-0.5">{description}</p>}
        </div>
        <div>{children}</div>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-between py-3 border-b border-slate-700 last:border-0">
      <div className="flex-1 pr-4">
        <label className="text-sm font-medium text-white block">{label}</label>
        {description && <p className="text-xs text-slate-400 mt-0.5">{description}</p>}
      </div>
      <div className="flex-shrink-0">{children}</div>
    </div>
  );
};

export default SettingItem;

