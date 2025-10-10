import { Switch } from '@headlessui/react';
import { cn } from '../../lib/utils';

interface ToggleProps {
  enabled: boolean;
  onChange: (enabled: boolean) => void;
  label?: string;
  description?: string;
  disabled?: boolean;
}

/**
 * Toggle Switch Component
 * 
 * Accessible toggle switch using HeadlessUI
 */
const Toggle: React.FC<ToggleProps> = ({
  enabled,
  onChange,
  label,
  description,
  disabled = false,
}) => {
  return (
    <Switch.Group>
      <div className="flex items-center justify-between">
        <div className="flex-1">
          {label && (
            <Switch.Label className="text-sm font-medium text-white cursor-pointer">
              {label}
            </Switch.Label>
          )}
          {description && (
            <Switch.Description className="text-xs text-slate-400 mt-0.5">
              {description}
            </Switch.Description>
          )}
        </div>
        <Switch
          checked={enabled}
          onChange={onChange}
          disabled={disabled}
          className={cn(
            enabled ? 'bg-blue-600' : 'bg-slate-700',
            disabled && 'opacity-50 cursor-not-allowed',
            'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-slate-900'
          )}
        >
          <span
            className={cn(
              enabled ? 'translate-x-6' : 'translate-x-1',
              'inline-block h-4 w-4 transform rounded-full bg-white transition-transform'
            )}
          />
        </Switch>
      </div>
    </Switch.Group>
  );
};

export default Toggle;

