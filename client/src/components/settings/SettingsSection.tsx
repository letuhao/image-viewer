import { ReactNode, useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface SettingsSectionProps {
  title: string;
  description?: string;
  children: ReactNode;
  defaultOpen?: boolean;
  collapsible?: boolean;
}

/**
 * Settings Section Component
 * 
 * Collapsible section for grouping related settings
 */
const SettingsSection: React.FC<SettingsSectionProps> = ({
  title,
  description,
  children,
  defaultOpen = true,
  collapsible = true,
}) => {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <div className="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
      {/* Section Header */}
      <div
        className={cn(
          'px-6 py-4 border-b border-slate-700',
          collapsible && 'cursor-pointer hover:bg-slate-750 transition-colors'
        )}
        onClick={() => collapsible && setIsOpen(!isOpen)}
      >
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold text-white">{title}</h3>
            {description && <p className="text-sm text-slate-400 mt-1">{description}</p>}
          </div>
          {collapsible && (
            <button className="p-1 text-slate-400 hover:text-white transition-colors">
              {isOpen ? (
                <ChevronDown className="h-5 w-5" />
              ) : (
                <ChevronRight className="h-5 w-5" />
              )}
            </button>
          )}
        </div>
      </div>

      {/* Section Content */}
      {isOpen && (
        <div className="px-6 py-4 space-y-4">
          {children}
        </div>
      )}
    </div>
  );
};

// Import cn utility
import { cn } from '../../lib/utils';

export default SettingsSection;

