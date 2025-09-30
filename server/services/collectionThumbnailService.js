const fs = require('fs-extra');
const path = require('path');
const sharp = require('sharp');
const db = require('../database');

class CollectionThumbnailService {
  constructor() {
    this.supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp'];
    this.thumbnailOptions = {
      width: 800,
      height: 450, // 16:9 aspect ratio
      quality: 85,
      format: 'jpeg'
    };
  }

  /**
   * Generate collection thumbnail using smart selection algorithm
   * @param {string} collectionId - Collection ID
   * @param {string} collectionPath - Path to collection
   * @param {string} collectionType - Type of collection (folder, zip, etc.)
   * @param {Object} options - Thumbnail generation options
   * @returns {Promise<string|null>} - Path to generated thumbnail or null
   */
  async generateCollectionThumbnail(collectionId, collectionPath, collectionType, options = {}) {
    try {
      console.log(`[COLLECTION_THUMBNAIL] Generating thumbnail for collection ${collectionId}`);
      
      const thumbnailOptions = { ...this.thumbnailOptions, ...options };
      
      // Get images from collection
      const images = await this.getCollectionImages(collectionId, collectionPath, collectionType);
      
      if (images.length === 0) {
        console.log(`[COLLECTION_THUMBNAIL] No images found in collection ${collectionId}`);
        return null;
      }

      // Smart selection algorithm
      const selectedImage = this.selectBestImage(images, collectionType);
      console.log(`[COLLECTION_THUMBNAIL] Selected image: ${selectedImage.filename} (${selectedImage.width}x${selectedImage.height})`);

      // Generate thumbnail
      const thumbnailPath = await this.createThumbnail(selectedImage, collectionId, thumbnailOptions);
      
      // Update collection with thumbnail URL
      await this.updateCollectionThumbnail(collectionId, thumbnailPath);
      
      console.log(`[COLLECTION_THUMBNAIL] Generated thumbnail: ${thumbnailPath}`);
      return thumbnailPath;
      
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error generating thumbnail for collection ${collectionId}:`, error);
      return null;
    }
  }

  /**
   * Get images from collection (from database or scan)
   */
  async getCollectionImages(collectionId, collectionPath, collectionType) {
    try {
      // First try to get from database
      const dbImages = await db.getImages(collectionId, { limit: 100 });
      
      if (dbImages && dbImages.length > 0) {
        console.log(`[COLLECTION_THUMBNAIL] Found ${dbImages.length} images in database`);
        return dbImages.map(img => ({
          filename: img.filename,
          path: img.relative_path,
          fullPath: collectionType === 'folder' ? path.join(collectionPath, img.relative_path) : null,
          width: img.width,
          height: img.height,
          file_size: img.file_size
        }));
      }

      // Fallback: scan collection for images
      console.log(`[COLLECTION_THUMBNAIL] No images in database, scanning collection`);
      return await this.scanCollectionForImages(collectionPath, collectionType);
      
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error getting collection images:`, error);
      return [];
    }
  }

  /**
   * Scan collection directory for images
   */
  async scanCollectionForImages(collectionPath, collectionType) {
    const images = [];
    
    try {
      if (collectionType === 'folder') {
        await this.scanFolder(collectionPath, images);
      } else {
        // For compressed files, scan ZIP contents
        console.log(`[COLLECTION_THUMBNAIL] Scanning compressed file: ${collectionPath}`);
        await this.scanCompressedFile(collectionPath, images);
      }
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error scanning collection:`, error);
    }
    
    return images;
  }

  /**
   * Scan compressed file (ZIP, 7Z, etc.) for images
   */
  async scanCompressedFile(filePath, images) {
    try {
      const StreamZip = require('node-stream-zip');
      const zip = new StreamZip.async({ file: filePath });
      
      const entries = await zip.entries();
      
      for (const [entryPath, entry] of Object.entries(entries)) {
        if (entry.isFile) {
          const ext = path.extname(entryPath).toLowerCase();
          if (this.supportedFormats.includes(ext)) {
            try {
              // Get file stats from ZIP entry
              const stats = entry;
              
              // Try to get image metadata without extracting
              const buffer = await zip.entryData(entryPath);
              const metadata = await sharp(buffer).metadata();
              
              images.push({
                filename: path.basename(entryPath),
                path: entryPath,
                fullPath: null, // Will be handled in createThumbnail
                width: metadata.width,
                height: metadata.height,
                file_size: stats.size
              });
            } catch (error) {
              console.warn(`[COLLECTION_THUMBNAIL] Skipping image ${entryPath}:`, error.message);
            }
          }
        }
      }
      
      await zip.close();
      console.log(`[COLLECTION_THUMBNAIL] Found ${images.length} images in compressed file`);
      
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error scanning compressed file ${filePath}:`, error);
    }
  }

  /**
   * Recursively scan folder for images
   */
  async scanFolder(folderPath, images, maxDepth = 3, currentDepth = 0) {
    if (currentDepth >= maxDepth) return;
    
    try {
      const items = await fs.readdir(folderPath, { withFileTypes: true });
      
      for (const item of items) {
        const fullPath = path.join(folderPath, item.name);
        
        if (item.isDirectory()) {
          await this.scanFolder(fullPath, images, maxDepth, currentDepth + 1);
        } else if (item.isFile()) {
          const ext = path.extname(item.name).toLowerCase();
          if (this.supportedFormats.includes(ext)) {
            try {
              const stats = await fs.stat(fullPath);
              const metadata = await sharp(fullPath).metadata();
              
              images.push({
                filename: item.name,
                path: fullPath,
                fullPath: fullPath,
                width: metadata.width,
                height: metadata.height,
                file_size: stats.size
              });
            } catch (error) {
              // Skip images that can't be processed
              console.warn(`[COLLECTION_THUMBNAIL] Skipping image ${item.name}:`, error.message);
            }
          }
        }
      }
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error scanning folder ${folderPath}:`, error);
    }
  }

  /**
   * Smart image selection algorithm
   * Priority: 1) Best aspect ratio 2) Good resolution 3) File size 4) Position
   */
  selectBestImage(images, collectionType) {
    if (images.length === 0) return null;
    if (images.length === 1) return images[0];

    // Filter out images that are too small
    const validImages = images.filter(img => 
      img.width >= 300 && img.height >= 200
    );

    if (validImages.length === 0) {
      // Fallback to any image if none meet minimum size
      return images[0];
    }

    // Calculate scores for each image
    const scoredImages = validImages.map(img => {
      const score = this.calculateImageScore(img);
      return { ...img, score };
    });

    // Sort by score (highest first)
    scoredImages.sort((a, b) => b.score - a.score);

    return scoredImages[0];
  }

  /**
   * Calculate image score based on multiple factors
   */
  calculateImageScore(image) {
    let score = 0;

    // 1. Aspect ratio score (16:9 = 100 points)
    const aspectRatio = image.width / image.height;
    const targetRatio = 16 / 9;
    const ratioDiff = Math.abs(aspectRatio - targetRatio);
    const ratioScore = Math.max(0, 100 - (ratioDiff * 200));
    score += ratioScore * 0.4; // 40% weight

    // 2. Resolution score (higher resolution = higher score)
    const resolution = image.width * image.height;
    const resolutionScore = Math.min(100, (resolution / (1920 * 1080)) * 100);
    score += resolutionScore * 0.3; // 30% weight

    // 3. File size score (reasonable size gets higher score)
    const fileSizeMB = image.file_size / (1024 * 1024);
    let sizeScore = 0;
    if (fileSizeMB >= 0.5 && fileSizeMB <= 5) {
      sizeScore = 100; // Optimal size
    } else if (fileSizeMB < 0.5) {
      sizeScore = 50; // Too small
    } else {
      sizeScore = Math.max(0, 100 - ((fileSizeMB - 5) * 10)); // Too large
    }
    score += sizeScore * 0.2; // 20% weight

    // 4. Position score (prefer images from beginning/middle)
    // This would need the image index, so we'll skip for now
    score += 50; // Base score

    return score;
  }

  /**
   * Create thumbnail from selected image
   */
  async createThumbnail(imageInfo, collectionId, options) {
    try {
      // Get cache path for collection thumbnail
      const cacheManager = require('./cacheManager');
      const filename = `collection_thumbnail.${options.format}`;
      const thumbnailPath = await cacheManager.getCachePath(collectionId, filename, 'thumbnail');

      // Ensure directory exists
      await fs.ensureDir(path.dirname(thumbnailPath));

      // Get image buffer based on collection type
      let imageBuffer;
      const collection = await db.getCollection(collectionId);
      
      if (collection.type === 'folder') {
        // For folders, read directly from file system
        imageBuffer = await fs.readFile(imageInfo.fullPath);
      } else {
        // For compressed files, extract from ZIP
        const StreamZip = require('node-stream-zip');
        const zip = new StreamZip.async({ file: collection.path });
        
        try {
          imageBuffer = await zip.entryData(imageInfo.path);
        } finally {
          await zip.close();
        }
      }

      // Generate thumbnail
      await sharp(imageBuffer)
        .resize(options.width, options.height, {
          fit: 'cover',
          position: 'center'
        })
        .jpeg({ quality: options.quality })
        .toFile(thumbnailPath);

      return thumbnailPath;
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error creating thumbnail:`, error);
      throw error;
    }
  }

  /**
   * Update collection with thumbnail URL
   */
  async updateCollectionThumbnail(collectionId, thumbnailPath) {
    try {
      // Convert path to URL
      const thumbnailUrl = `/api/collections/${collectionId}/thumbnail`;
      
      await db.updateCollection(collectionId, {
        thumbnail_path: thumbnailPath,
        thumbnail_url: thumbnailUrl,
        thumbnail_generated_at: new Date()
      });

      console.log(`[COLLECTION_THUMBNAIL] Updated collection ${collectionId} with thumbnail URL: ${thumbnailUrl}`);
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error updating collection thumbnail:`, error);
      throw error;
    }
  }

  /**
   * Regenerate thumbnail for existing collection
   */
  async regenerateCollectionThumbnail(collectionId) {
    try {
      const collection = await db.getCollection(collectionId);
      if (!collection) {
        throw new Error(`Collection ${collectionId} not found`);
      }

      return await this.generateCollectionThumbnail(
        collectionId,
        collection.path,
        collection.type
      );
    } catch (error) {
      console.error(`[COLLECTION_THUMBNAIL] Error regenerating thumbnail:`, error);
      throw error;
    }
  }

  /**
   * Batch regenerate thumbnails for multiple collections
   */
  async batchRegenerateThumbnails(collectionIds, progressCallback = null) {
    const results = {
      total: collectionIds.length,
      success: 0,
      failed: 0,
      errors: []
    };

    for (let i = 0; i < collectionIds.length; i++) {
      const collectionId = collectionIds[i];
      
      try {
        await this.regenerateCollectionThumbnail(collectionId);
        results.success++;
        
        if (progressCallback) {
          progressCallback({
            current: i + 1,
            total: results.total,
            collectionId,
            status: 'success'
          });
        }
      } catch (error) {
        results.failed++;
        results.errors.push({
          collectionId,
          error: error.message
        });
        
        if (progressCallback) {
          progressCallback({
            current: i + 1,
            total: results.total,
            collectionId,
            status: 'error',
            error: error.message
          });
        }
      }
    }

    return results;
  }
}

module.exports = new CollectionThumbnailService();
