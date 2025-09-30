import React, { useRef } from 'react';
import {
  FolderIcon,
  DocumentIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline';

interface PathSelectorProps {
  value: string;
  onChange: (path: string) => void;
  placeholder?: string;
  type?: 'folder' | 'file';
  accept?: string;
  label?: string;
  required?: boolean;
  className?: string;
}

const PathSelector: React.FC<PathSelectorProps> = ({
  value,
  onChange,
  placeholder = "Enter path manually",
  type = 'folder',
  accept,
  label,
  required = false,
  className = "",
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleBrowseClick = async () => {
    if (type === 'folder') {
      if ('showDirectoryPicker' in window) {
        try {
          const dirHandle = await (window as any).showDirectoryPicker();
          if (dirHandle && dirHandle.name) {
            // Browser security prevents getting the absolute path
            // Leave the input unchanged and prompt user to enter the full path manually
            const message = `Đã chọn folder: "${dirHandle.name}"\n\nDo hạn chế bảo mật của trình duyệt, bạn cần nhập đầy đủ đường dẫn thủ công vào ô nhập.\nVí dụ: D:\\Cache\\${dirHandle.name}`;
            alert(message);
          }
        } catch (err) {
          // User cancelled or an error occurred; do nothing
        }
      } else {
        window.alert('Trình duyệt không hỗ trợ chọn thư mục. Vui lòng nhập đường dẫn thủ công.');
      }
      return;
    }

    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (files && files.length > 0) {
      if (type === 'folder') {
        // For folder selection with webkitdirectory
        const folderPath = files[0].webkitRelativePath;
        if (folderPath) {
          // Extract just the folder name from the path
          const folderName = folderPath.split('/')[0];
          onChange(folderName);
        }
      } else {
        // For file selection - just get the filename
        onChange(files[0].name);
      }
    }
    
    // Reset the input
    event.target.value = '';
  };

  const clearPath = () => {
    onChange('');
  };

  return (
    <div className={className}>
      {label && (
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
          {label}
          {required && <span className="text-red-500 ml-1">*</span>}
        </label>
      )}
      
      <div className="flex gap-2">
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white dark:bg-dark-600 text-gray-900 dark:text-white"
          required={required}
        />
        
        {value && (
          <button
            type="button"
            onClick={clearPath}
            className="px-3 py-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
          >
            <XMarkIcon className="h-4 w-4" />
          </button>
        )}
        
        <button
          type="button"
          onClick={handleBrowseClick}
          className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {type === 'folder' ? (
            <FolderIcon className="h-4 w-4 mr-2" />
          ) : (
            <DocumentIcon className="h-4 w-4 mr-2" />
          )}
          Browse
        </button>
      </div>

      {/* Hidden file input for file selection only */}
      {type !== 'folder' && (
        <input
          ref={fileInputRef}
          type="file"
          accept={accept}
          onChange={handleFileSelect}
          className="hidden"
        />
      )}
    </div>
  );
};

export default PathSelector;
