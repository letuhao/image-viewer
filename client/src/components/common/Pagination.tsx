import React from 'react';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

export interface PaginationSettings {
  showFirstLast: boolean;
  showPageNumbers: boolean;
  pageNumbersToShow: number; // How many page numbers to show on each side
}

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  hasPrevious?: boolean;
  hasNext?: boolean;
  settings?: PaginationSettings;
  compact?: boolean;
}

const defaultSettings: PaginationSettings = {
  showFirstLast: true,
  showPageNumbers: true,
  pageNumbersToShow: 5,
};

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  hasPrevious = currentPage > 1,
  hasNext = currentPage < totalPages,
  settings = defaultSettings,
  compact = false,
}) => {
  const mergedSettings = { ...defaultSettings, ...settings };

  // Generate page numbers to display
  const getPageNumbers = (): (number | 'ellipsis')[] => {
    if (!mergedSettings.showPageNumbers) return [];

    const { pageNumbersToShow } = mergedSettings;
    const pages: (number | 'ellipsis')[] = [];

    if (totalPages <= pageNumbersToShow * 2 + 1) {
      // Show all pages if total is small
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Show pages around current page
      const leftBound = Math.max(1, currentPage - pageNumbersToShow);
      const rightBound = Math.min(totalPages, currentPage + pageNumbersToShow);

      // Always show first page
      if (leftBound > 1) {
        pages.push(1);
        if (leftBound > 2) {
          pages.push('ellipsis');
        }
      }

      // Show pages around current
      for (let i = leftBound; i <= rightBound; i++) {
        pages.push(i);
      }

      // Always show last page
      if (rightBound < totalPages) {
        if (rightBound < totalPages - 1) {
          pages.push('ellipsis');
        }
        pages.push(totalPages);
      }
    }

    return pages;
  };

  const pageNumbers = getPageNumbers();

  const buttonClass = "p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors";
  const activePageClass = "px-2 py-1 rounded bg-primary-600 text-white text-xs font-medium";
  const inactivePageClass = "px-2 py-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 text-xs transition-colors cursor-pointer";
  const ellipsisClass = "px-2 py-1 text-slate-600 text-xs";

  return (
    <div className={`flex items-center ${compact ? 'gap-1' : 'gap-2'}`}>
      {/* First Page Button */}
      {mergedSettings.showFirstLast && (
        <button
          onClick={() => onPageChange(1)}
          disabled={!hasPrevious}
          className={buttonClass}
          title="First Page"
        >
          <ChevronsLeft className="h-4 w-4" />
        </button>
      )}

      {/* Previous Button */}
      <button
        onClick={() => onPageChange(Math.max(1, currentPage - 1))}
        disabled={!hasPrevious}
        className={buttonClass}
        title="Previous Page"
      >
        <ChevronLeft className="h-4 w-4" />
      </button>

      {/* Page Number Input */}
      <div className="flex items-center gap-1">
        <input
          type="number"
          min="1"
          max={totalPages}
          value={currentPage}
          onChange={(e) => {
            const newPage = parseInt(e.target.value);
            if (newPage >= 1 && newPage <= totalPages) {
              onPageChange(newPage);
            }
          }}
          className="w-16 px-2 py-1 bg-slate-700 border border-slate-600 rounded text-white text-xs text-center focus:outline-none focus:ring-1 focus:ring-primary-500"
          title="Go to page"
        />
        <span className="text-xs text-slate-400">/</span>
        <span className="text-xs text-slate-400 min-w-[2rem] text-center">{totalPages}</span>
      </div>

      {/* Page Numbers */}
      {mergedSettings.showPageNumbers && pageNumbers.length > 0 && (
        <div className="flex items-center gap-1">
          {pageNumbers.map((pageNum, index) => {
            if (pageNum === 'ellipsis') {
              return (
                <span key={`ellipsis-${index}`} className={ellipsisClass}>
                  ...
                </span>
              );
            }

            return (
              <button
                key={pageNum}
                onClick={() => onPageChange(pageNum)}
                className={pageNum === currentPage ? activePageClass : inactivePageClass}
                title={`Go to page ${pageNum}`}
              >
                {pageNum}
              </button>
            );
          })}
        </div>
      )}

      {/* Next Button */}
      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={!hasNext}
        className={buttonClass}
        title="Next Page"
      >
        <ChevronRight className="h-4 w-4" />
      </button>

      {/* Last Page Button */}
      {mergedSettings.showFirstLast && (
        <button
          onClick={() => onPageChange(totalPages)}
          disabled={!hasNext}
          className={buttonClass}
          title="Last Page"
        >
          <ChevronsRight className="h-4 w-4" />
        </button>
      )}
    </div>
  );
};

// Note: Pagination settings are now configured in the Settings page
// Navigate to Settings > User Preferences > Pagination Controls to customize

