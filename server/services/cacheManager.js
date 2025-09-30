const fs = require('fs-extra');
const path = require('path');
const sharp = require('sharp');
const db = require('../database');

class CacheManager {
  constructor() {
    this.cacheFolders = new Map();
    this.loadCacheFolders();
  }

  async loadCacheFolders() {
    try {
      const folders = await db.getCacheFolders();
      this.cacheFolders.clear();
      if (folders && folders.length > 0) {
        folders.forEach(folder => {
          this.cacheFolders.set(folder.id, folder);
        });
      }
    } catch (error) {
      console.error('Error loading cache folders:', error);
      // Initialize with empty cache folders if none exist
      this.cacheFolders.clear();
    }
  }

  async getCacheFolderForCollection(collectionId) {
    try {
      // Try to get existing cache folder for this collection
      let cacheFolder = await db.getCollectionCacheFolder(collectionId);
      
      if (cacheFolder && cacheFolder.is_active) {
        return cacheFolder;
      }

      // Get the best cache folder using distribution logic
      cacheFolder = await db.getCacheFolderForCollection(collectionId);
      
      // Update local cache
      if (cacheFolder) {
        this.cacheFolders.set(cacheFolder.id, cacheFolder);
      }
      
      return cacheFolder;
    } catch (error) {
      console.error('Error getting cache folder for collection:', error);
      return null;
    }
  }

  async getCachePath(collectionId, filename, type = 'thumbnail') {
    try {
      const cacheFolder = await this.getCacheFolderForCollection(collectionId);
      
      if (!cacheFolder) {
        // No assigned folder and none available → do not fallback to local cache
        console.error(`[CACHE] No cache folders available for collection ${collectionId}. Aborting path resolution.`);
        throw new Error('No cache folders available');
      }

      // Create collection-specific cache directory
      const collectionCacheDir = path.join(cacheFolder.path, collectionId);
      await fs.ensureDir(collectionCacheDir);
      
      return path.join(collectionCacheDir, filename);
    } catch (error) {
      console.error('Error getting cache path:', error);
      // Do not fallback to default cache; propagate to caller
      throw error;
    }
  }

  async generateThumbnail(imageInput, collectionId, imageId, options = {}) {
    try {
      const {
        width = 300,
        height = 300,
        quality = 80,
        format = 'jpeg'
      } = options;

      const filename = `${imageId}_thumb.${format}`;
      const thumbnailPath = await this.getCachePath(collectionId, filename, 'thumbnail');
      
      // Check if thumbnail already exists
      if (await fs.pathExists(thumbnailPath)) {
        return thumbnailPath;
      }

      // Ensure directory exists
      await fs.ensureDir(path.dirname(thumbnailPath));

      // Generate thumbnail - handle both file paths and buffers
      let sharpInstance;
      if (Buffer.isBuffer(imageInput)) {
        sharpInstance = sharp(imageInput);
      } else {
        sharpInstance = sharp(imageInput);
      }

      await sharpInstance
        .resize(width, height, { 
          fit: 'inside',
          withoutEnlargement: true 
        })
        .jpeg({ quality })
        .toFile(thumbnailPath);

      // Update cache folder usage statistics
      const cacheFolder = await this.getCacheFolderForCollection(collectionId);
      if (cacheFolder) {
        const stats = await fs.stat(thumbnailPath);
        await db.updateCacheFolderUsage(cacheFolder.id, stats.size, 1);
      }

      return thumbnailPath;
    } catch (error) {
      console.error('Error generating thumbnail:', error);
      throw error;
    }
  }

  async cacheImage(imagePath, collectionId, imageId, options = {}) {
    try {
      const {
        maxWidth = 1920,
        maxHeight = 1920,
        quality = 85,
        format = 'jpeg'
      } = options;

      const filename = `${imageId}_cached.${format}`;
      const cachedPath = await this.getCachePath(collectionId, filename, 'image');
      
      // Check if cached image already exists
      if (await fs.pathExists(cachedPath)) {
        return cachedPath;
      }

      // Ensure directory exists
      await fs.ensureDir(path.dirname(cachedPath));

      // Process and cache image
      await sharp(imagePath)
        .resize(maxWidth, maxHeight, { 
          fit: 'inside',
          withoutEnlargement: true 
        })
        .jpeg({ quality })
        .toFile(cachedPath);

      // Update cache folder usage statistics
      const cacheFolder = await this.getCacheFolderForCollection(collectionId);
      if (cacheFolder) {
        const stats = await fs.stat(cachedPath);
        await db.updateCacheFolderUsage(cacheFolder.id, stats.size, 1);
      }

      return cachedPath;
    } catch (error) {
      console.error('Error caching image:', error);
      throw error;
    }
  }

  async getCachedImage(collectionId, imageId, type = 'thumbnail') {
    try {
      const format = type === 'thumbnail' ? 'jpeg' : 'jpeg';
      const filename = `${imageId}_${type === 'thumbnail' ? 'thumb' : 'cached'}.${format}`;
      const cachedPath = await this.getCachePath(collectionId, filename, type);
      
      if (await fs.pathExists(cachedPath)) {
        return cachedPath;
      }
      
      return null;
    } catch (error) {
      console.error('Error getting cached image:', error);
      return null;
    }
  }

  async deleteCachedImage(collectionId, imageId) {
    try {
      const cacheFolder = await this.getCacheFolderForCollection(collectionId);
      
      if (cacheFolder) {
        const collectionCacheDir = path.join(cacheFolder.path, collectionId);
        const thumbnailPath = path.join(collectionCacheDir, `${imageId}_thumb.jpeg`);
        const cachedPath = path.join(collectionCacheDir, `${imageId}_cached.jpeg`);
        
        let totalSize = 0;
        
        // Delete thumbnail
        if (await fs.pathExists(thumbnailPath)) {
          const stats = await fs.stat(thumbnailPath);
          totalSize += stats.size;
          await fs.remove(thumbnailPath);
        }
        
        // Delete cached image
        if (await fs.pathExists(cachedPath)) {
          const stats = await fs.stat(cachedPath);
          totalSize += stats.size;
          await fs.remove(cachedPath);
        }
        
        // Update cache folder usage statistics
        if (totalSize > 0) {
          await db.updateCacheFolderUsage(cacheFolder.id, -totalSize, -1);
        }
      }
    } catch (error) {
      console.error('Error deleting cached image:', error);
    }
  }

  async deleteCollectionCache(collectionId) {
    try {
      const cacheFolder = await this.getCacheFolderForCollection(collectionId);
      
      if (cacheFolder) {
        const collectionCacheDir = path.join(cacheFolder.path, collectionId);
        
        if (await fs.pathExists(collectionCacheDir)) {
          // Calculate total size before deletion
          let totalSize = 0;
          let fileCount = 0;
          
          const files = await fs.readdir(collectionCacheDir, { withFileTypes: true });
          for (const file of files) {
            if (file.isFile()) {
              const filePath = path.join(collectionCacheDir, file.name);
              const stats = await fs.stat(filePath);
              totalSize += stats.size;
              fileCount++;
            }
          }
          
          // Delete the entire collection cache directory
          await fs.remove(collectionCacheDir);
          
          // Update cache folder usage statistics
          await db.updateCacheFolderUsage(cacheFolder.id, -totalSize, -fileCount);
        }
      }
    } catch (error) {
      console.error('Error deleting collection cache:', error);
    }
  }

  async getCacheStatistics() {
    try {
      const stats = await db.getCacheFolderStats();
      return stats;
    } catch (error) {
      console.error('Error getting cache statistics:', error);
      return null;
    }
  }

  async cleanupExpiredCache() {
    try {
      const folders = await db.getCacheFolders();
      let totalCleaned = 0;
      
      for (const folder of folders) {
        try {
          // Clean up empty directories
          const folderPath = folder.path;
          if (await fs.pathExists(folderPath)) {
            const dirs = await fs.readdir(folderPath, { withFileTypes: true });
            
            for (const dir of dirs) {
              if (dir.isDirectory()) {
                const dirPath = path.join(folderPath, dir.name);
                const files = await fs.readdir(dirPath);
                
                if (files.length === 0) {
                  await fs.remove(dirPath);
                  totalCleaned++;
                }
              }
            }
          }
        } catch (error) {
          console.error(`Error cleaning cache folder ${folder.name}:`, error);
        }
      }
      
      return totalCleaned;
    } catch (error) {
      console.error('Error cleaning up expired cache:', error);
      return 0;
    }
  }

  async redistributeCache() {
    try {
      const collections = await db.getAllCollections();
      let redistributed = 0;
      
      for (const collection of collections) {
        // Get current cache folder
        const currentFolder = await db.getCollectionCacheFolder(collection.id);
        
        // Get optimal cache folder
        const optimalFolder = await db.getCacheFolderForCollection(collection.id);
        
        if (currentFolder && optimalFolder && currentFolder.id !== optimalFolder.id) {
          // Move cache to optimal folder
          await this.moveCollectionCache(collection.id, currentFolder.id, optimalFolder.id);
          redistributed++;
        }
      }
      
      return redistributed;
    } catch (error) {
      console.error('Error redistributing cache:', error);
      return 0;
    }
  }

  async moveCollectionCache(collectionId, fromFolderId, toFolderId) {
    try {
      const fromFolder = this.cacheFolders.get(fromFolderId);
      const toFolder = this.cacheFolders.get(toFolderId);
      
      if (!fromFolder || !toFolder) {
        throw new Error('Source or destination cache folder not found');
      }
      
      const fromPath = path.join(fromFolder.path, collectionId);
      const toPath = path.join(toFolder.path, collectionId);
      
      if (await fs.pathExists(fromPath)) {
        // Calculate size before moving
        let totalSize = 0;
        let fileCount = 0;
        
        const files = await fs.readdir(fromPath, { withFileTypes: true });
        for (const file of files) {
          if (file.isFile()) {
            const filePath = path.join(fromPath, file.name);
            const stats = await fs.stat(filePath);
            totalSize += stats.size;
            fileCount++;
          }
        }
        
        // Ensure destination directory exists
        await fs.ensureDir(path.dirname(toPath));
        
        // Move the cache directory
        await fs.move(fromPath, toPath);
        
        // Update cache folder usage statistics
        await db.updateCacheFolderUsage(fromFolderId, -totalSize, -fileCount);
        await db.updateCacheFolderUsage(toFolderId, totalSize, fileCount);
        
        // Update collection binding
        await db.bindCollectionToCacheFolder(collectionId, toFolderId);
      }
    } catch (error) {
      console.error('Error moving collection cache:', error);
      throw error;
    }
  }
}

// Export singleton instance
const cacheManager = new CacheManager();

module.exports = cacheManager;
