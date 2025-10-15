import { useState } from 'react';
import { Trash2, Eye, AlertTriangle, CheckCircle, XCircle } from 'lucide-react';
import Button from '../ui/Button';
import LoadingSpinner from '../ui/LoadingSpinner';
import toast from 'react-hot-toast';

interface MacOSXCleanupPreview {
  success: boolean;
  duration: string;
  affectedCollections: number;
  totalImagesToRemove: number;
  totalThumbnailsToRemove: number;
  totalCacheImagesToRemove: number;
  totalSpaceToFree: number;
  errors: string[];
  affectedCollectionDetails: Array<{
    collectionId: string;
    collectionName: string;
    collectionPath: string;
    imagesToRemove: number;
    thumbnailsToRemove: number;
    cacheImagesToRemove: number;
    spaceToFree: number;
  }>;
}

interface MacOSXCleanupResult {
  success: boolean;
  duration: string;
  affectedCollections: number;
  totalImagesRemoved: number;
  totalThumbnailsRemoved: number;
  totalCacheImagesRemoved: number;
  totalSpaceFreed: number;
  errors: string[];
  affectedCollectionDetails: Array<{
    collectionId: string;
    collectionName: string;
    collectionPath: string;
    imagesRemoved: number;
    thumbnailsRemoved: number;
    cacheImagesRemoved: number;
    spaceFreed: number;
  }>;
}

const MacOSXCleanup: React.FC = () => {
  const [preview, setPreview] = useState<MacOSXCleanupPreview | null>(null);
  const [isLoadingPreview, setIsLoadingPreview] = useState(false);
  const [isLoadingCleanup, setIsLoadingCleanup] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const handlePreview = async () => {
    setIsLoadingPreview(true);
    try {
      const response = await fetch('/api/v1/systemsettings/cleanup/macosx/preview');
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      setPreview(data);
      setShowPreview(true);
      
      if (data.affectedCollections === 0) {
        toast.success('No __MACOSX files found to clean up!');
      } else {
        toast.success(`Found ${data.affectedCollections} collections with __MACOSX files`);
      }
    } catch (error) {
      console.error('Failed to preview MacOSX cleanup:', error);
      toast.error('Failed to preview cleanup. Please try again.');
    } finally {
      setIsLoadingPreview(false);
    }
  };

  const handleCleanup = async () => {
    if (!preview) return;
    
    const confirmMessage = `Are you sure you want to clean up __MACOSX files?\n\n` +
      `This will remove:\n` +
      `• ${preview.totalImagesToRemove} images\n` +
      `• ${preview.totalThumbnailsToRemove} thumbnails\n` +
      `• ${preview.totalCacheImagesToRemove} cache images\n` +
      `• ${formatBytes(preview.totalSpaceToFree)} of disk space\n` +
      `• From ${preview.affectedCollections} collections\n\n` +
      `This action cannot be undone!`;
    
    if (!window.confirm(confirmMessage)) {
      return;
    }

    setIsLoadingCleanup(true);
    try {
      const response = await fetch('/api/v1/systemsettings/cleanup/macosx', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const result: MacOSXCleanupResult = await response.json();
      
      if (result.success) {
        toast.success(
          `Cleanup completed! Removed ${result.totalImagesRemoved} images, ${result.totalThumbnailsRemoved} thumbnails, ${result.totalCacheImagesRemoved} cache images, freed ${formatBytes(result.totalSpaceFreed)}`,
          { duration: 5000 }
        );
        
        // Refresh preview to show updated state
        setShowPreview(false);
        setPreview(null);
      } else {
        toast.error(`Cleanup completed with errors: ${result.errors.join(', ')}`);
      }
    } catch (error) {
      console.error('Failed to cleanup MacOSX files:', error);
      toast.error('Failed to cleanup __MACOSX files. Please try again.');
    } finally {
      setIsLoadingCleanup(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="bg-slate-800 rounded-lg p-6">
        <div className="flex items-start space-x-3">
          <AlertTriangle className="h-6 w-6 text-yellow-500 mt-1 flex-shrink-0" />
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-white mb-2">
              __MACOSX File Cleanup
            </h3>
            <p className="text-slate-300 mb-4">
              Clean up macOS metadata files (__MACOSX) from your collections. These files are created when 
              macOS archives are created and can cause processing issues on Windows systems.
            </p>
            
            <div className="flex space-x-3">
              <Button
                variant="secondary"
                onClick={handlePreview}
                disabled={isLoadingPreview || isLoadingCleanup}
                className="flex items-center space-x-2"
              >
                {isLoadingPreview ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
                <span>{isLoadingPreview ? 'Scanning...' : 'Preview Cleanup'}</span>
              </Button>
            </div>
          </div>
        </div>
      </div>

      {showPreview && preview && (
        <div className="bg-slate-800 rounded-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h4 className="text-lg font-semibold text-white">Cleanup Preview</h4>
            <button
              onClick={() => setShowPreview(false)}
              className="text-slate-400 hover:text-white"
            >
              <XCircle className="h-5 w-5" />
            </button>
          </div>

          {preview.affectedCollections === 0 ? (
            <div className="flex items-center space-x-3 text-green-400">
              <CheckCircle className="h-6 w-6" />
              <span className="text-lg">No __MACOSX files found to clean up!</span>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                <div className="bg-slate-700 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-blue-400">{preview.affectedCollections}</div>
                  <div className="text-sm text-slate-300">Collections</div>
                </div>
                <div className="bg-slate-700 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-red-400">{preview.totalImagesToRemove}</div>
                  <div className="text-sm text-slate-300">Images</div>
                </div>
                <div className="bg-slate-700 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-orange-400">{preview.totalThumbnailsToRemove}</div>
                  <div className="text-sm text-slate-300">Thumbnails</div>
                </div>
                <div className="bg-slate-700 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-purple-400">{formatBytes(preview.totalSpaceToFree)}</div>
                  <div className="text-sm text-slate-300">Space to Free</div>
                </div>
              </div>

              <div className="mb-6">
                <h5 className="text-md font-semibold text-white mb-3">Affected Collections</h5>
                <div className="max-h-60 overflow-y-auto space-y-2">
                  {preview.affectedCollectionDetails.map((detail) => (
                    <div key={detail.collectionId} className="bg-slate-700 rounded-lg p-3">
                      <div className="flex items-center justify-between">
                        <div>
                          <div className="font-medium text-white">{detail.collectionName}</div>
                          <div className="text-sm text-slate-400 truncate">{detail.collectionPath}</div>
                        </div>
                        <div className="text-right text-sm">
                          <div className="text-red-400">
                            {detail.imagesToRemove} images, {detail.thumbnailsToRemove} thumbnails
                          </div>
                          <div className="text-purple-400">
                            {formatBytes(detail.spaceToFree)}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex space-x-3">
                <Button
                  variant="destructive"
                  onClick={handleCleanup}
                  disabled={isLoadingCleanup}
                  className="flex items-center space-x-2"
                >
                  {isLoadingCleanup ? (
                    <LoadingSpinner size="sm" />
                  ) : (
                    <Trash2 className="h-4 w-4" />
                  )}
                  <span>{isLoadingCleanup ? 'Cleaning...' : 'Clean Up Now'}</span>
                </Button>
                <Button
                  variant="ghost"
                  onClick={() => setShowPreview(false)}
                >
                  Cancel
                </Button>
              </div>
            </>
          )}

          {preview.errors.length > 0 && (
            <div className="mt-4 p-4 bg-red-900/20 border border-red-500/20 rounded-lg">
              <h5 className="text-red-400 font-semibold mb-2">Errors:</h5>
              <ul className="text-red-300 text-sm space-y-1">
                {preview.errors.map((error, index) => (
                  <li key={index}>• {error}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default MacOSXCleanup;
