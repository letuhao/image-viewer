const sharp = require('sharp');
const fs = require('fs-extra');
const path = require('path');
const crypto = require('crypto');
const Logger = require('../utils/logger');

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
      
      // Ensure cache directory exists
      this.logger.debug('Ensuring cache directory exists', { collectionCacheDir });
      await fs.ensureDir(collectionCacheDir);
      this.logger.debug('Cache directory ensured');

      // Check if collection needs to be scanned first
      this.logger.debug('Checking collection scan status', { collectionId: collection.id });
      const needsScan = await this.checkIfCollectionNeedsScan(collection);
      
      if (needsScan) {
        this.logger.info('Collection needs scanning, starting scan process', { 
          collectionId: collection.id,
          collectionName: collection.name 
        });
        
        // Update progress to show scanning
        this.progress.currentImage = 'Scanning collection...';
        
        // Scan the collection
        await this.scanCollection(collection);
        this.logger.info('Collection scan completed', { collectionId: collection.id });
      }

      // Get all images for this collection
      this.logger.debug('Fetching collection images', { collectionId: collection.id });
      const images = await this.getCollectionImages(collection.id);
      this.logger.info('Images fetched', { 
        collectionId: collection.id,
        imageCount: images.length 
      });
      
      for (const image of images) {
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
          await this.processImage(image, collection, collectionCacheDir, options);
          this.logger.debug('Image processed successfully', { filename: image.filename });
        } catch (error) {
          this.logger.error('Error processing image', {
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
        imagesProcessed: images.length
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

  async processImage(image, collection, cachePath, options) {
    const { quality, maxWidth, maxHeight, format, overwrite } = options;
    
    try {
      // Get original image data
      const imageData = await this.getImageData(collection, image);
      
      if (!imageData) {
        throw new Error('Could not retrieve image data');
      }

      // Process image with Sharp
      let sharpInstance = sharp(imageData);

      // Get image metadata
      const metadata = await sharpInstance.metadata();
      
      // Generate cache filename with metadata
      const cacheFilename = this.generateCacheFilename(image, options, metadata);
      const cacheFilePath = path.join(cachePath, cacheFilename);

      // Check if cache already exists
      if (!overwrite && await fs.pathExists(cacheFilePath)) {
        console.log(`[CACHE-GEN] Cache exists for ${image.filename}, skipping`);
        return;
      }
      
      // Apply transformations - ONLY change quality, preserve everything else
      if (format !== 'original') {
        // Only apply quality compression, no resizing to preserve aspect ratio
        switch (format) {
          case 'webp':
            sharpInstance = sharpInstance.webp({ 
              quality,
              effort: 6 // Higher effort for better compression
            });
            break;
          case 'jpeg':
          default:
            sharpInstance = sharpInstance.jpeg({ 
              quality,
              progressive: true // Progressive JPEG for better web loading
            });
            break;
        }
      }

      // Write processed image to cache
      await sharpInstance.toFile(cacheFilePath);
      
      // Get final image dimensions after processing
      const finalMetadata = await sharp(cacheFilePath).metadata();
      
      // Get cache file size
      const cacheFileSize = (await fs.stat(cacheFilePath)).size;

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

      console.log(`[CACHE-GEN] ✅ Cached ${image.filename} -> ${cacheFilename} (${finalMetadata.width}x${finalMetadata.height})`);

    } catch (error) {
      console.error(`[CACHE-GEN] Error processing image ${image.filename}:`, error);
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
