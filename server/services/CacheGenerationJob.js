const sharp = require('sharp');
const fs = require('fs-extra');
const path = require('path');
const crypto = require('crypto');
const Logger = require('../utils/logger');
const longPathHandler = require('../utils/longPathHandler');

class CacheGenerationJob {
  constructor(jobId, options) {
    this.jobId = jobId;
    this.options = options;
    this.isRunning = false;
    this.isCancelled = false;
    this.progress = {
      total: 0,
      completed: 0,
      currentCollection: null,
      currentImage: null,
      errors: []
    };
    this.logger = new Logger('CacheGenerationJob');
    this.startTime = Date.now();
    
    // Process concurrency control to prevent file handle conflicts
    this.maxConcurrentProcesses = parseInt(process.env.MAX_CONCURRENT_CACHE_PROCESSES || '1');
    this.activeProcesses = 0;
    this.processQueue = [];
    
    // Network drive error tracking
    this.networkDriveErrors = 0;
    this.maxNetworkDriveErrors = 5;
    this.networkDriveChecked = false;
  }

  async start() {
    this.isRunning = true;
    this.logger.flow('JOB_STARTED', { jobId: this.jobId, startTime: new Date().toISOString() });
    this.logger.info('Job initialization', this.options);
    
    try {
      const { collectionIds, quality, format, overwrite, collections } = this.options;
      
      this.logger.info('Options extracted', { 
        collectionIds, 
        quality, 
        format, 
        overwrite, 
        collectionCount: collections.length 
      });
      
        // Calculate total images (including collections that need scanning)
        this.logger.flow('CALCULATING_TOTAL_IMAGES');
        this.progress.total = collections.reduce((total, collection) => {
          const imageCount = collection.settings?.total_images || 0;
          const needsScan = (imageCount === 0 || !collection.settings?.last_scanned);
          
          this.logger.debug(`Collection analysis: ${collection.name}`, { 
            imageCount, 
            collectionId: collection.id,
            needsScan,
            settings: collection.settings 
          });
          
          // If collection needs scan, we'll estimate based on file size or use a default
          // For now, we'll use the existing count and let the scan process update it
          return total + Math.max(imageCount, needsScan ? 1 : 0); // At least 1 step for scanning
        }, 0);

      this.logger.info('Total calculation complete', { 
        totalImages: this.progress.total, 
        collectionCount: collections.length 
      });

      // Check if we should use parallel processing
      const useParallelProcessing = process.env.ENABLE_PARALLEL_CACHE_PROCESSING === 'true';
      
      const processOptions = { quality, format, overwrite, maxWidth: null, maxHeight: null };
      
      if (useParallelProcessing) {
        this.logger.info('Using parallel processing for cache generation');
        await this.processCollectionsParallel(collections, processOptions);
      } else {
        this.logger.info('Using sequential processing for cache generation');
        await this.processCollectionsSequential(collections, processOptions);
      }

      if (!this.isCancelled) {
        this.logger.flow('JOB_COMPLETED_SUCCESSFULLY');
        this.logger.perf('Total job execution', this.startTime);
        this.progress.status = 'completed';
      } else {
        this.logger.flow('JOB_CANCELLED');
        this.progress.status = 'cancelled';
      }

    } catch (error) {
      this.logger.error('Job failed', { 
        error: error.message, 
        stack: error.stack,
        jobId: this.jobId 
      });
      this.progress.status = 'failed';
      this.progress.errors.push({
        message: error.message,
        stack: error.stack,
        timestamp: new Date().toISOString()
      });
    } finally {
      this.isRunning = false;
      this.logger.flow('JOB_FINISHED', { 
        status: this.progress.status,
        totalDuration: Date.now() - this.startTime 
      });
    }
  }

  async processCollectionsSequential(collections, options) {
    const { quality, format, overwrite } = options;
    
    for (const collection of collections) {
      if (this.isCancelled) {
        this.logger.warn('Job cancelled by user');
        break;
      }

      this.progress.currentCollection = collection.name;
      this.logger.flow('PROCESSING_COLLECTION', { 
        collectionName: collection.name,
        collectionId: collection.id 
      });

      const processOptions = { quality, format, overwrite, maxWidth: null, maxHeight: null };
      this.logger.debug('Process options for collection', processOptions);
      
      await this.processCollection(collection, processOptions);
    }
  }

  async processCollectionsParallel(collections, options) {
    const { quality, format, overwrite } = options;
    
    // Group collections by drive
    const collectionsByDrive = await this.groupCollectionsByDrive(collections);
    
    this.logger.info('Collections grouped by drive', {
      driveCount: Object.keys(collectionsByDrive).length,
      drives: Object.keys(collectionsByDrive).map(drive => ({
        drive,
        collectionCount: collectionsByDrive[drive].length
      }))
    });

    // Process each drive in parallel (1 process per drive)
    const drivePromises = Object.entries(collectionsByDrive).map(async ([drive, driveCollections]) => {
      this.logger.info(`Starting parallel processing for drive ${drive}`, {
        drive,
        collectionCount: driveCollections.length
      });

      const processOptions = { quality, format, overwrite, maxWidth: null, maxHeight: null };
      
      // Process collections sequentially within each drive (1 process per drive)
      for (const collection of driveCollections) {
        if (this.isCancelled) {
          this.logger.warn(`Job cancelled, stopping drive ${drive}`);
          break;
        }

        this.progress.currentCollection = collection.name;
        this.logger.flow('PROCESSING_COLLECTION_PARALLEL', { 
          collectionName: collection.name,
          collectionId: collection.id,
          drive
        });

        await this.processCollection(collection, processOptions);
      }

      this.logger.info(`Completed parallel processing for drive ${drive}`, {
        drive,
        collectionCount: driveCollections.length
      });
    });

    // Wait for all drives to complete
    await Promise.all(drivePromises);
    
    this.logger.info('All drives completed parallel processing');
  }

  async groupCollectionsByDrive(collections) {
    const collectionsByDrive = {};
    
    for (const collection of collections) {
      try {
        // Get cache folder for this collection
        const cacheFolder = await this.getCollectionCacheFolder(collection.id);
        const drive = this.extractDriveFromPath(cacheFolder);
        
        if (!collectionsByDrive[drive]) {
          collectionsByDrive[drive] = [];
        }
        
        collectionsByDrive[drive].push(collection);
        
        this.logger.debug('Collection assigned to drive', {
          collectionId: collection.id,
          collectionName: collection.name,
          drive,
          cacheFolder
        });
      } catch (error) {
        this.logger.error('Failed to get cache folder for collection', {
          collectionId: collection.id,
          collectionName: collection.name,
          error: error.message
        });
        // Fallback to default drive
        const defaultDrive = 'D';
        if (!collectionsByDrive[defaultDrive]) {
          collectionsByDrive[defaultDrive] = [];
        }
        collectionsByDrive[defaultDrive].push(collection);
      }
    }
    
    return collectionsByDrive;
  }

  extractDriveFromPath(cacheFolderPath) {
    if (!cacheFolderPath) return 'D'; // Default drive
    
    // Extract drive letter from path (e.g., "I:\Image_Cache" -> "I")
    const driveMatch = cacheFolderPath.match(/^([A-Z]):/i);
    return driveMatch ? driveMatch[1].toUpperCase() : 'D';
  }

  async processCollection(collection, options) {
    const collectionStartTime = Date.now();
    const { quality, maxWidth, maxHeight, format, overwrite } = options;
    
    this.logger.flow('PROCESS_COLLECTION_START', { 
      collectionId: collection.id,
      collectionName: collection.name,
      options 
    });
    
    try {
      // Get collection cache folder
      this.logger.debug('Getting cache folder for collection', { collectionId: collection.id });
      const collectionCacheDir = await this.getCollectionCacheFolder(collection.id);
      this.logger.info('Cache folder obtained', { collectionCacheDir });
      
      // Check network drive connectivity once per collection (not per image)
      if (!this.networkDriveChecked && collectionCacheDir && collectionCacheDir.includes(':') && collectionCacheDir[1] === ':') {
        try {
          await this.ensureNetworkDriveReady(collectionCacheDir);
          this.networkDriveChecked = true;
        } catch (error) {
          this.logger.warn('Network drive check failed, continuing with processing', {
            cacheDir: collectionCacheDir,
            error: error.message
          });
        }
      }
      
      // Ensure cache directory exists
      this.logger.debug('Ensuring cache directory exists', { collectionCacheDir });
      await fs.ensureDir(collectionCacheDir);
      this.logger.debug('Cache directory ensured');

      // Handle scan logic based on overwrite setting
      if (overwrite) {
        // Overwrite = true: Rebuild from scratch, force rescan
        this.logger.info('Overwrite enabled, forcing rescan to rebuild from scratch', { 
          collectionId: collection.id,
          collectionName: collection.name 
        });
        
        this.progress.currentImage = 'Rebuilding collection...';
        await this.scanCollection(collection);
        this.logger.info('Collection rebuild completed', { collectionId: collection.id });
      } else {
        // Overwrite = false: Check if collection needs scanning
        this.logger.debug('Checking collection scan status', { collectionId: collection.id });
        const needsScan = await this.checkIfCollectionNeedsScan(collection);
        
        if (needsScan) {
          this.logger.info('Collection needs scanning, starting scan process', { 
            collectionId: collection.id,
            collectionName: collection.name 
          });
          
          this.progress.currentImage = 'Scanning collection...';
          await this.scanCollection(collection);
          this.logger.info('Collection scan completed', { collectionId: collection.id });
        }
      }

      // Get all images for this collection
      this.logger.debug('Fetching collection images', { collectionId: collection.id });
      let images = await this.getCollectionImages(collection.id);
      this.logger.info('Images fetched', { 
        collectionId: collection.id,
        imageCount: images.length,
        images: images.length > 0 ? images.slice(0, 3).map(img => ({ id: img.id, filename: img.filename })) : 'No images found'
      });
      
      // If no images found but collection has total_images > 0, force rescan
      if (images.length === 0 && collection.settings?.total_images > 0) {
        this.logger.warn('Collection has metadata but no images in database, forcing rescan', {
          collectionId: collection.id,
          totalImagesInSettings: collection.settings.total_images,
          lastScanned: collection.settings.last_scanned
        });
        
        this.progress.currentImage = 'Rescanning collection...';
        await this.scanCollection(collection);
        
        // Fetch images again after rescan
        images = await this.getCollectionImages(collection.id);
        this.logger.info('Images fetched after rescan', { 
          collectionId: collection.id,
          imageCount: images.length,
          images: images.length > 0 ? images.slice(0, 3).map(img => ({ id: img.id, filename: img.filename })) : 'Still no images found'
        });
      }
      
      // If still no images, skip processing
      if (images.length === 0) {
        this.logger.warn('No images found after rescan, skipping collection', {
          collectionId: collection.id,
          collectionName: collection.name
        });
        return;
      }
      
      // Filter images based on overwrite setting
      let imagesToProcess = images;
      if (!overwrite) {
        // Overwrite = false: Filter out already cached images
        imagesToProcess = images.filter(image => {
          const isCached = image.cache_path && image.cache_filename && image.cached_at;
          if (isCached) {
            this.logger.debug('Skipping already cached image', { 
              filename: image.filename,
              cachedAt: image.cached_at
            });
          }
          return !isCached;
        });
        
        this.logger.info('Filtered images for processing', {
          totalImages: images.length,
          cachedImages: images.length - imagesToProcess.length,
          imagesToProcess: imagesToProcess.length
        });
      } else {
        this.logger.info('Processing all images (overwrite enabled)', {
          totalImages: images.length
        });
        
        // Overwrite = true: Clean up old cache files first
        await this.cleanupOldCacheFiles(collection, collectionCacheDir, options);
      }

      for (const image of imagesToProcess) {
        if (this.isCancelled) {
          this.logger.warn('Collection processing cancelled');
          break;
        }

        this.progress.currentImage = image.filename;
        this.logger.debug('Processing image', { 
          filename: image.filename,
          imageId: image.id 
        });
        
        try {
          await this.processWithConcurrencyLimit(image, collection, collectionCacheDir, options);
          this.logger.debug('Image processed successfully', { filename: image.filename });
        } catch (error) {
          this.logger.error('Error processing image after retries', {
            filename: image.filename,
            error: error.message,
            stack: error.stack
          });
          this.progress.errors.push({
            collection: collection.name,
            image: image.filename,
            message: error.message,
            timestamp: new Date().toISOString()
          });
        }

        this.progress.completed++;
        
        // Update progress every 10 images
        if (this.progress.completed % 10 === 0) {
          const percentage = Math.round((this.progress.completed / this.progress.total) * 100);
          this.logger.info('Progress update', {
            completed: this.progress.completed,
            total: this.progress.total,
            percentage
          });
        }
      }

      this.logger.perf('Collection processing', collectionStartTime, {
        collectionId: collection.id,
        totalImages: images.length,
        imagesProcessed: imagesToProcess.length,
        cachedImagesSkipped: images.length - imagesToProcess.length
      });

    } catch (error) {
      this.logger.error('Error processing collection', {
        collectionId: collection.id,
        collectionName: collection.name,
        error: error.message,
        stack: error.stack
      });
      throw error;
    }
  }

  async cleanupOldCacheFiles(collection, cacheDir, options) {
    const { format, quality } = options;
    
    try {
      this.logger.info('Starting cleanup of old cache files', {
        collectionId: collection.id,
        cacheDir
      });
      
      // Get all files in cache directory
      const cacheFiles = await longPathHandler.readDirSafe(cacheDir);
      
      // Filter cache files (exclude thumbnails and collection_thumbnail)
      // Remove ALL cache files regardless of quality/format to avoid duplicates
      const imageCacheFiles = cacheFiles.filter(file => 
        (file.includes('_q') && file.includes('_jpeg')) ||
        (file.includes('_q') && file.includes('_webp')) ||
        (file.includes('_q') && file.includes('_png'))
      ).filter(file => 
        !file.includes('_thumb') && 
        !file.includes('collection_thumbnail')
      );
      
      this.logger.debug('Found old cache files to clean up', {
        totalFiles: cacheFiles.length,
        cacheFiles: imageCacheFiles.length,
        files: imageCacheFiles.slice(0, 5) // Show first 5 files
      });
      
      // Remove old cache files
      let removedCount = 0;
      for (const file of imageCacheFiles) {
        try {
          const filePath = longPathHandler.joinSafe(cacheDir, file);
          await longPathHandler.removeSafe(filePath);
          removedCount++;
          
          if (removedCount % 50 === 0) {
            this.logger.debug(`Cleaned up ${removedCount} old cache files`);
          }
        } catch (error) {
          this.logger.warn('Failed to remove old cache file', {
            file,
            error: error.message
          });
        }
      }
      
      this.logger.info('Old cache files cleanup completed', {
        removedCount,
        totalFiles: imageCacheFiles.length
      });
      
      // Update database - clear cache records for all images in collection
      const images = await this.getCollectionImages(collection.id);
      for (const image of images) {
        try {
          await this.updateImageCacheRecord(image.id, {
            cache_path: null,
            cache_filename: null,
            cached_at: null,
            cache_size: null,
            cache_width: null,
            cache_height: null
          });
        } catch (error) {
          this.logger.warn('Failed to clear cache record in database', {
            imageId: image.id,
            filename: image.filename,
            error: error.message
          });
        }
      }
      
      this.logger.info('Database cache records cleared', {
        collectionId: collection.id,
        imagesUpdated: images.length
      });
      
    } catch (error) {
      this.logger.error('Error during cache cleanup', {
        collectionId: collection.id,
        cacheDir,
        error: error.message,
        stack: error.stack
      });
      // Don't throw - continue with processing even if cleanup fails
    }
  }

  async processWithConcurrencyLimit(image, collection, cachePath, options) {
    return new Promise((resolve, reject) => {
      const processTask = async () => {
        try {
          await this.processImageWithRetry(image, collection, cachePath, options);
          resolve();
        } catch (error) {
          reject(error);
        } finally {
          this.activeProcesses--;
          this.processNextInQueue();
        }
      };

      if (this.activeProcesses < this.maxConcurrentProcesses) {
        this.activeProcesses++;
        processTask();
      } else {
        this.processQueue.push(processTask);
      }
    });
  }

  processNextInQueue() {
    if (this.processQueue.length > 0 && this.activeProcesses < this.maxConcurrentProcesses) {
      const nextTask = this.processQueue.shift();
      this.activeProcesses++;
      nextTask();
    }
  }

  async processImageWithRetry(image, collection, cachePath, options, maxRetries = 5) {
    let lastError;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        this.logger.debug(`Processing image attempt ${attempt}/${maxRetries}`, { 
          filename: image.filename,
          cachePath
        });
        
        await this.processImage(image, collection, cachePath, options);
        return; // Success, exit retry loop
        
      } catch (error) {
        lastError = error;
        
        // Check if it's a Windows network drive error
        const isNetworkDriveError = error.message.includes('The device does not recognize the command') ||
                                   error.message.includes('unable to open for write') ||
                                   error.message.includes('network') ||
                                   error.message.includes('drive') ||
                                   error.message.includes('device') ||
                                   error.message.includes('not recognize');
        
        if (isNetworkDriveError && attempt < maxRetries) {
          // Track network drive errors
          this.networkDriveErrors++;
          
          // Reduced delay: 1s, 2s, 4s for network drive issues (faster retry)
          const delay = Math.min(1000 * Math.pow(2, attempt - 1), 4000); // Max 4s instead of 16s
          
          this.logger.warn(`Windows network drive error on attempt ${attempt}, retrying in ${delay}ms`, {
            filename: image.filename,
            cachePath,
            error: error.message,
            attempt,
            maxRetries,
            errorType: 'NETWORK_DRIVE_ERROR',
            totalNetworkErrors: this.networkDriveErrors
          });
          
          // If too many network drive errors, reduce concurrency
          if (this.networkDriveErrors >= this.maxNetworkDriveErrors && this.maxConcurrentProcesses > 1) {
            this.maxConcurrentProcesses = 1;
            this.logger.warn('Reducing concurrency to 1 due to network drive issues', {
              totalNetworkErrors: this.networkDriveErrors,
              maxConcurrentProcesses: this.maxConcurrentProcesses
            });
          }
          
          // For network drive errors, also try to verify drive connectivity
          if (attempt === 2) {
            await this.checkDriveConnectivity(cachePath);
          }
          
          await new Promise(resolve => setTimeout(resolve, delay));
          continue;
        }
        
        // If not a network error or max retries reached, throw immediately
        throw error;
      }
    }
    
    // If we get here, all retries failed
    this.logger.error(`All retry attempts failed for image`, {
      filename: image.filename,
      cachePath,
      finalError: lastError.message,
      maxRetries
    });
    throw lastError;
  }

  async checkDriveConnectivity(cachePath) {
    try {
      const path = require('path');
      const fs = require('fs-extra');
      
      // Extract drive letter from cache path
      const driveLetter = path.parse(cachePath).root;
      const testPath = path.join(driveLetter, 'test_connectivity.tmp');
      
      this.logger.debug('Checking drive connectivity', { driveLetter, testPath });
      
      // Try to write and immediately delete a test file
      await fs.writeFile(testPath, 'connectivity test');
      await fs.remove(testPath);
      
      this.logger.debug('Drive connectivity check passed', { driveLetter });
      return true;
      
    } catch (error) {
      this.logger.warn('Drive connectivity check failed', {
        cachePath,
        error: error.message,
        errorType: 'DRIVE_CONNECTIVITY_FAILED'
      });
      return false;
    }
  }

  async ensureNetworkDriveReady(cachePath, maxAttempts = 3) {
    const path = require('path');
    const fs = require('fs-extra');
    
    // Extract drive letter from cache path
    const driveLetter = path.parse(cachePath).root;
    
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        // Check if drive is accessible
        await fs.access(driveLetter, fs.constants.F_OK);
        
        // Try to create the cache directory if it doesn't exist
        await fs.ensureDir(cachePath);
        
        // Test write capability with a small file
        const testFile = path.join(cachePath, '.cache_test.tmp');
        await fs.writeFile(testFile, 'test');
        await fs.remove(testFile);
        
        this.logger.debug('Network drive ready', { driveLetter, cachePath });
        return true;
        
      } catch (error) {
        const isNetworkError = error.message.includes('The device does not recognize the command') ||
                              error.message.includes('network') ||
                              error.message.includes('drive') ||
                              error.message.includes('device');
        
        if (isNetworkError && attempt < maxAttempts) {
          const delay = 1000 * attempt; // 1s, 2s, 3s
          this.logger.warn(`Network drive not ready, retrying in ${delay}ms`, {
            driveLetter,
            cachePath,
            attempt,
            maxAttempts,
            error: error.message
          });
          
          await new Promise(resolve => setTimeout(resolve, delay));
          continue;
        }
        
        this.logger.error('Network drive not accessible', {
          driveLetter,
          cachePath,
          error: error.message,
          attempt,
          maxAttempts
        });
        throw error;
      }
    }
  }

  async processImage(image, collection, cachePath, options) {
    const { quality, maxWidth, maxHeight, format, overwrite } = options;
    
    try {
      // Get original image data
      const imageData = await this.getImageData(collection, image);
      
      if (!imageData) {
        throw new Error('Could not retrieve image data');
      }

      // Process image with Sharp - with memory and concurrency limits
      let sharpInstance = sharp(imageData, {
        // Limit memory usage to prevent buffer overflow
        limitInputPixels: 268402689, // ~268MP (default is 268402689)
        sequentialRead: true, // Read sequentially to reduce memory usage
        density: 72 // Set default density to avoid issues
      });

      // Get image metadata with timeout
      const metadata = await Promise.race([
        sharpInstance.metadata(),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Metadata timeout')), 10000)
        )
      ]);
      
      // Check if we should skip processing (only for 'original' format)
      if (this.shouldSkipProcessing(metadata, quality, format)) {
        this.logger.info('Skipping processing - target format is original', {
          filename: image.filename,
          originalQuality: metadata.quality,
          targetQuality: quality,
          format: format
        });
        
        // Copy original file to cache with original quality info
        await this.copyOriginalToCache(image, collection, cachePath, metadata, quality, format);
        return;
      }
      
      // Generate cache filename with metadata
      const cacheFilename = this.generateCacheFilename(image, options, metadata);
      const cacheFilePath = path.join(cachePath, cacheFilename);
      
      // Validate and handle long paths for Windows compatibility
      if (longPathHandler.isPathTooLong(cacheFilePath)) {
        this.logger.warn('Cache file path too long, using safe path', {
          originalPath: cacheFilePath,
          originalLength: cacheFilePath.length,
          safePath: longPathHandler.getSafePath(cacheFilePath)
        });
        cacheFilePath = longPathHandler.getSafePath(cacheFilePath);
      }
      
      // Check available disk space (basic check)
      try {
        await longPathHandler.statSafe(cachePath);
        this.logger.debug('Cache directory accessible', { cachePath });
      } catch (error) {
        throw new Error(`Cache directory not accessible: ${error.message}`);
      }

      // Check if cache already exists
      if (!overwrite && await longPathHandler.pathExistsSafe(cacheFilePath)) {
        this.logger.info('Cache exists, skipping', { imageId: image.id, cacheFilePath });
        return;
      }
      
      // Apply transformations - ONLY change quality, preserve everything else
      if (format !== 'original') {
        // Only apply quality compression, no resizing to preserve aspect ratio
        switch (format) {
          case 'webp':
            sharpInstance = sharpInstance.webp({ 
              quality,
              effort: 4, // Balanced effort (1-6, 6 is slowest but best compression)
              smartSubsample: true, // Better quality for small files
              reductionEffort: 6 // Better compression
            });
            break;
          case 'jpeg':
          default:
            sharpInstance = sharpInstance.jpeg({ 
              quality,
              progressive: true, // Progressive JPEG for better web loading
              mozjpeg: true, // Use mozjpeg encoder if available (better compression)
              optimiseScans: true, // Optimize scan order
              trellisQuantisation: true, // Better quality
              overshootDeringing: true, // Reduce ringing artifacts
              optimizeScans: true // Optimize scan order for progressive
            });
            break;
        }
      }

      // Write processed image to cache with error handling
      try {
        await sharpInstance.toFile(cacheFilePath);
        this.logger.debug('Cache file written successfully', { 
          filename: image.filename,
          cacheFilePath 
        });
      } catch (writeError) {
        this.logger.error('Failed to write cache file', {
          filename: image.filename,
          cacheFilePath,
          error: writeError.message
        });
        
        // Check if file was created but is empty (0 bytes)
        try {
          const stats = await longPathHandler.statSafe(cacheFilePath);
          if (stats.size === 0) {
            this.logger.warn('Cache file created but is empty, removing it', { 
              filename: image.filename,
              cacheFilePath 
            });
            await longPathHandler.removeSafe(cacheFilePath);
          }
        } catch (statError) {
          // File doesn't exist or can't be accessed, ignore
        }
        
        throw writeError;
      }
      
      // Get final image dimensions after processing
      const finalMetadata = await sharp(cacheFilePath).metadata();
      
      // Get cache file size
      const cacheFileSize = (await longPathHandler.statSafe(cacheFilePath)).size;

      // Update image cache record in database
      await this.updateImageCacheRecord(image.id, {
        cache_path: cacheFilePath,
        cache_filename: cacheFilename,
        cache_size: cacheFileSize,
        cache_quality: quality,
        cache_format: format,
        // Keep original dimensions since we're not resizing
        cache_dimensions: `${metadata.width}x${metadata.height}`,
        cached_at: new Date()
      });

      // Update cache folder usage statistics
      await this.updateCacheFolderUsage(cachePath, cacheFileSize, 1);

      this.logger.info('Image cached successfully', {
        originalFilename: image.filename,
        cacheFilename: cacheFilename,
        dimensions: `${finalMetadata.width}x${finalMetadata.height}`
      });

    } catch (error) {
      this.logger.error('Error processing image', {
        filename: image.filename,
        error: error.message,
        stack: error.stack
      });
      throw error;
    }
  }

  generateCacheFilename(image, options, metadata) {
    const { quality, format } = options;
    const ext = format === 'original' ? path.extname(image.filename) : `.${format}`;
    const nameWithoutExt = path.parse(image.filename).name;
    
    // Simple filename with quality info only
    const qualitySuffix = quality < 100 ? `_q${quality}` : '';
    const formatSuffix = format !== 'original' ? `_${format}` : '';
    
    return `${nameWithoutExt}${qualitySuffix}${formatSuffix}${ext}`;
  }

  async getCollectionCacheFolder(collectionId) {
    this.logger.flow('GET_COLLECTION_CACHE_FOLDER_START', { collectionId });
    
    try {
      // Use the existing cache folder distribution system
      this.logger.debug('Calling db.getCacheFolderForCollection', { collectionId });
      const db = require('../database');
      
      this.logger.debug('Database instance obtained', { dbExists: !!db });
      const cacheFolder = await db.getCacheFolderForCollection(collectionId);
      this.logger.info('Cache folder query result', { 
        collectionId,
        cacheFolder: cacheFolder ? {
          id: cacheFolder.id,
          name: cacheFolder.name,
          path: cacheFolder.path,
          priority: cacheFolder.priority,
          is_active: cacheFolder.is_active
        } : null
      });
      
      if (!cacheFolder) {
        this.logger.error('No cache folder available', { collectionId });
        throw new Error(`No cache folder available for collection ${collectionId}`);
      }
      
      // Create collection-specific subdirectory in the cache folder
      const collectionCacheDir = path.join(cacheFolder.path, collectionId);
      this.logger.debug('Creating collection cache directory', { 
        collectionId,
        cacheFolderPath: cacheFolder.path,
        collectionCacheDir 
      });
      
      await fs.ensureDir(collectionCacheDir);
      this.logger.info('Collection cache directory created', { collectionCacheDir });
      
      return collectionCacheDir;
    } catch (error) {
      this.logger.error('Failed to get cache folder for collection', {
        collectionId,
        error: error.message,
        stack: error.stack
      });
      
      // Don't use fallback, throw the error instead
      throw new Error(`Failed to get cache folder for collection ${collectionId}: ${error.message}`);
    }
  }

  async getCollectionImages(collectionId) {
    // Get images from database
    const db = require('../database');
    return await db.getImages(collectionId, { limit: 10000 }); // Get all images
  }

  async getImageData(collection, image) {
    // This would get the actual image data from the collection
    // For ZIP files, extract the image
    // For folders, read the file directly
    
    if (collection.type === 'zip') {
      return await this.extractImageFromZip(collection.path, image.filename);
    } else {
      return await fs.readFile(path.join(collection.path, image.filename));
    }
  }

  async extractImageFromZip(zipPath, filename) {
    const StreamZip = require('node-stream-zip');
    const zip = new StreamZip.async({ file: zipPath });
    
    try {
      const entry = await zip.entry(filename);
      if (!entry) {
        throw new Error(`Image ${filename} not found in ZIP`);
      }
      
      return await zip.entryData(filename);
    } finally {
      await zip.close();
    }
  }

  async updateImageCacheRecord(imageId, cacheData) {
    this.logger.debug('Updating image cache record in database', { imageId, cacheData });
    const db = require('../database');
    // Update image record with cache information
    try {
      await db.updateImage(imageId, cacheData);
      this.logger.debug('Updated cache record for image', { imageId });
    } catch (error) {
      this.logger.error('Failed to update cache record for image', { imageId, error: error.message, stack: error.stack });
    }
  }

    shouldSkipProcessing(metadata, targetQuality, targetFormat) {
      this.logger.debug('Checking if should skip processing', {
        originalQuality: metadata.quality,
        targetQuality: targetQuality,
        originalFormat: metadata.format,
        targetFormat: targetFormat
      });
      
      // Skip if target format is 'original' - this is the only case we truly skip
      if (targetFormat === 'original') {
        this.logger.debug('Target format is original, skipping processing');
        return true;
      }
      
      // ALWAYS process to reduce file size - cache purpose is optimization, not quality preservation
      // Even high quality originals (4K, etc.) should be processed to reduce file size for faster loading
      this.logger.debug('Processing needed - cache purpose is file size reduction', {
        originalQuality: metadata.quality,
        targetQuality: targetQuality,
        originalFormat: metadata.format,
        targetFormat: targetFormat,
        reason: 'Cache purpose is to reduce file size for faster loading'
      });
      
      return false;
    }

    async copyOriginalToCache(image, collection, cachePath, metadata, quality, format) {
      this.logger.flow('COPY_ORIGINAL_TO_CACHE_START', { 
        imageId: image.id, 
        filename: image.filename,
        originalQuality: metadata.quality 
      });
      
      try {
        // Generate cache filename
        const cacheFilename = this.generateCacheFilename(image, { quality, format }, metadata);
        const cacheFilePath = path.join(cachePath, cacheFilename);
        
        // Get original file path
        let originalFilePath;
        if (collection.type === 'folder') {
          originalFilePath = path.join(collection.path, image.relative_path);
        } else if (['zip', '7z', 'rar', 'tar'].includes(collection.type)) {
          // For compressed files, we need to extract to a temp location first
          const tempDir = path.join(cachePath, 'temp');
          await fs.ensureDir(tempDir);
          const tempFilePath = path.join(tempDir, image.filename);
          
          // Extract to temp file
          const StreamZip = require('node-stream-zip').async;
          const zip = new StreamZip.async({ file: collection.path });
          try {
            const entry = await zip.entry(image.relative_path);
            if (entry) {
              const data = await zip.entryData(entry);
              await fs.writeFile(tempFilePath, data);
              originalFilePath = tempFilePath;
            } else {
              throw new Error(`Image ${image.filename} not found in compressed file`);
            }
          } finally {
            await zip.close();
          }
        } else {
          throw new Error(`Unsupported collection type: ${collection.type}`);
        }
        
        // Copy original file to cache
        await fs.copy(originalFilePath, cacheFilePath);
        
        // Get file size
        const cacheFileSize = (await fs.stat(cacheFilePath)).size;
        
        // Clean up temp file if it exists
        if (collection.type !== 'folder') {
          const tempDir = path.join(cachePath, 'temp');
          await fs.remove(tempDir);
        }
        
        // Update image cache record in database
        await this.updateImageCacheRecord(image.id, {
          cache_path: cacheFilePath,
          cache_filename: cacheFilename,
          cache_size: cacheFileSize,
          cache_quality: metadata.quality || quality, // Use original quality
          cache_format: metadata.format || format,
          cache_dimensions: `${metadata.width}x${metadata.height}`,
          cached_at: new Date()
        });
        
        // Update cache folder usage statistics
        await this.updateCacheFolderUsage(cachePath, cacheFileSize, 1);
        
        this.logger.info('Original file copied to cache', {
          imageId: image.id,
          filename: image.filename,
          originalQuality: metadata.quality,
          cacheFileSize: cacheFileSize
        });
        
      } catch (error) {
        this.logger.error('Error copying original to cache', {
          imageId: image.id,
          filename: image.filename,
          error: error.message,
          stack: error.stack
        });
        throw error;
      } finally {
        this.logger.flow('COPY_ORIGINAL_TO_CACHE_END', { imageId: image.id });
      }
    }

    async updateCacheFolderUsage(cachePath, fileSize, fileCount) {
      this.logger.debug('Updating cache folder usage statistics', { cachePath, fileSize, fileCount });
      const db = require('../database');
      try {
        // Find the cache folder that contains this cache path
        const cacheFolder = await db.getCacheFolderByPath(cachePath);
        if (cacheFolder) {
          await db.updateCacheFolderUsage(cacheFolder.id, fileSize, fileCount);
          this.logger.info('Updated cache folder usage', { 
            cacheFolderId: cacheFolder.id, 
            cacheFolderName: cacheFolder.name, 
            sizeDelta: fileSize, 
            fileCountDelta: fileCount 
          });
        } else {
          this.logger.warn('No cache folder found for path to update usage', { cachePath });
        }
      } catch (error) {
        this.logger.error('Failed to update cache folder usage', { cachePath, error: error.message, stack: error.stack });
      }
    }

    async checkIfCollectionNeedsScan(collection) {
      this.logger.debug('Checking if collection needs scan', { 
        collectionId: collection.id,
        totalImages: collection.settings?.total_images || 0,
        lastScanned: collection.settings?.last_scanned 
      });
      
      // Collection needs scan if:
      // 1. No images found (total_images = 0)
      // 2. Never been scanned (no last_scanned date)
      const needsScan = (collection.settings?.total_images || 0) === 0 || !collection.settings?.last_scanned;
      
      this.logger.info('Collection scan check result', { 
        collectionId: collection.id,
        needsScan,
        reason: needsScan ? 'No images or never scanned' : 'Already scanned'
      });
      
      return needsScan;
    }

    async scanCollection(collection) {
      this.logger.flow('SCAN_COLLECTION_START', { 
        collectionId: collection.id,
        collectionName: collection.name,
        collectionPath: collection.path,
        collectionType: collection.type 
      });
      
      try {
        const db = require('../database');
        
        // Import the scan function from collections route
        const collectionsRoute = require('../routes/collections');
        const scanCollectionImages = collectionsRoute.scanCollectionImages;
        
        this.logger.debug('Starting collection scan', { 
          collectionId: collection.id,
          path: collection.path,
          type: collection.type 
        });
        
        // Call the existing scan function
        await scanCollectionImages(collection.id, collection.path, collection.type);
        
        this.logger.info('Collection scan completed successfully', { 
          collectionId: collection.id,
          collectionName: collection.name 
        });
        
      } catch (error) {
        this.logger.error('Error scanning collection', {
          collectionId: collection.id,
          collectionName: collection.name,
          error: error.message,
          stack: error.stack
        });
        throw error;
      } finally {
        this.logger.flow('SCAN_COLLECTION_END', { collectionId: collection.id });
      }
    }

    cancel() {
      this.isCancelled = true;
      this.logger.warn('Job cancellation requested', { jobId: this.jobId });
    }

    getProgress() {
      return {
        ...this.progress,
        percentage: this.progress.total > 0 ? Math.round((this.progress.completed / this.progress.total) * 100) : 0,
        isRunning: this.isRunning,
        isCancelled: this.isCancelled
      };
    }
}

module.exports = CacheGenerationJob;
