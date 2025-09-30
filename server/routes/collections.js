const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const StreamZip = require('node-stream-zip');
const sharp = require('sharp');
const mime = require('mime-types');
const db = require('../database');
const cacheManager = require('../services/cacheManager');
const collectionThumbnailService = require('../services/collectionThumbnailService');

// Supported image formats
const SUPPORTED_FORMATS = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.svg'];

// Supported compressed file formats
const COMPRESSED_FORMATS = ['.zip', '.cbz', '.cbr', '.7z', '.rar', '.tar', '.tar.gz', '.tar.bz2'];

// Get all collections with pagination support
router.get('/', async (req, res) => {
  try {
    const { page = 1, limit = 50, filter } = req.query;
    const pageNum = parseInt(page);
    const limitNum = parseInt(limit);
    const skip = (pageNum - 1) * limitNum;
    
    let collections = await db.getAllCollections();
    
    // Apply filter if provided
    if (filter === 'with-thumbnails') {
      collections = collections.filter(col => col.thumbnail_url);
    } else if (filter === 'without-thumbnails') {
      collections = collections.filter(col => !col.thumbnail_url);
    }
    
    const total = collections.length;
    const totalPages = Math.ceil(total / limitNum);
    
    // Apply pagination
    const paginatedCollections = collections.slice(skip, skip + limitNum);
    
    // Get statistics and tags for each collection
    const collectionsWithStats = await Promise.all(
      paginatedCollections.map(async (collection) => {
        const [stats, tags] = await Promise.all([
          db.getCollectionStats(collection.id),
          db.getCollectionTags(collection.id)
        ]);
        
        return {
          ...collection,
          statistics: stats || {
            view_count: 0,
            total_view_time: 0,
            search_count: 0,
            last_viewed: null,
            last_searched: null
          },
          tags: tags || []
        };
      })
    );
    
    res.json({
      collections: collectionsWithStats,
      pagination: {
        page: pageNum,
        limit: limitNum,
        total,
        totalPages,
        hasNext: pageNum < totalPages,
        hasPrev: pageNum > 1
      }
    });
  } catch (error) {
    console.error('Error fetching collections:', error);
    res.status(500).json({ error: 'Failed to fetch collections' });
  }
});

// Get specific collection
router.get('/:id', async (req, res) => {
  try {
    const collection = await db.getCollection(req.params.id);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    res.json(collection);
  } catch (error) {
    console.error('Error fetching collection:', error);
    res.status(500).json({ error: 'Failed to fetch collection' });
  }
});

// Add new collection
router.post('/', async (req, res) => {
  try {
    const { name, path: collectionPath, type } = req.body;
    
    if (!name || !collectionPath || !type) {
      return res.status(400).json({ error: 'Missing required fields' });
    }
    
    if (!['folder', 'zip', '7z', 'rar', 'tar'].includes(type)) {
      return res.status(400).json({ error: 'Invalid collection type' });
    }
    
    // Check if path exists
    const exists = await fs.pathExists(collectionPath);
    if (!exists) {
      return res.status(400).json({ error: 'Path does not exist' });
    }
    
    // Add collection to database
    const collectionId = await db.addCollection(name, collectionPath, type);
    
    // Start scanning images in background
    scanCollectionImages(collectionId, collectionPath, type).catch(console.error);
    
    // Generate collection thumbnail in background
    collectionThumbnailService.generateCollectionThumbnail(collectionId, collectionPath, type)
      .then(thumbnailPath => {
        if (thumbnailPath) {
          console.log(`[COLLECTION] Generated thumbnail for collection ${collectionId}: ${thumbnailPath}`);
        }
      })
      .catch(error => {
        console.error(`[COLLECTION] Failed to generate thumbnail for collection ${collectionId}:`, error);
      });
    
    res.json({ id: collectionId, message: 'Collection added successfully' });
  } catch (error) {
    console.error('Error adding collection:', error);
    res.status(500).json({ error: 'Failed to add collection' });
  }
});

// Update collection
router.put('/:id', async (req, res) => {
  try {
    const updates = req.body;
    const result = await db.updateCollection(req.params.id, updates);
    
    if (result === 0) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    res.json({ message: 'Collection updated successfully' });
  } catch (error) {
    console.error('Error updating collection:', error);
    res.status(500).json({ error: 'Failed to update collection' });
  }
});

// Delete collection
router.delete('/:id', async (req, res) => {
  try {
    const result = await db.deleteCollection(req.params.id);
    
    if (result === 0) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    res.json({ message: 'Collection deleted successfully' });
  } catch (error) {
    console.error('Error deleting collection:', error);
    res.status(500).json({ error: 'Failed to delete collection' });
  }
});

// Rescan collection images
router.post('/:id/scan', async (req, res) => {
  try {
    const collection = await db.getCollection(req.params.id);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Clear existing images
    await db.deleteImages(req.params.id);
    
    // Start scanning
    scanCollectionImages(collection.id, collection.path, collection.type).catch(console.error);
    
    res.json({ message: 'Collection scan started' });
  } catch (error) {
    console.error('Error scanning collection:', error);
    res.status(500).json({ error: 'Failed to scan collection' });
  }
});

// Get collection images with pagination
router.get('/:id/images', async (req, res) => {
  try {
    const { id } = req.params;
    if (!id || id === 'undefined') {
      return res.status(400).json({ error: 'Collection ID is required' });
    }
    
    const { page = 1, limit = 50, sort = 'filename', order = 'asc' } = req.query;
    const offset = (page - 1) * limit;
    
    console.log(`[DEBUG] Getting images for collection: ${id}`);
    const images = await db.getImages(id, { limit: parseInt(limit), offset });
    const total = await db.getImageCount(id);
    
    // Track collection view (only for first page to avoid spam)
    if (parseInt(page) === 1) {
      await db.incrementViewCount(id);
    }
    
    res.json({
      images,
      pagination: {
        page: parseInt(page),
        limit: parseInt(limit),
        total,
        pages: Math.ceil(total / limit)
      }
    });
  } catch (error) {
    console.error('Error fetching collection images:', error);
    res.status(500).json({ error: 'Failed to fetch images' });
  }
});

// Scan collection images function
async function scanCollectionImages(collectionId, collectionPath, type) {
  try {
    console.log(`Starting scan for collection ${collectionId}: ${collectionPath}`);
    
    let imageFiles = [];
    
    if (type === 'folder') {
      imageFiles = await scanFolder(collectionPath);
    } else if (['zip', '7z', 'rar', 'tar'].includes(type)) {
      imageFiles = await scanCompressedFile(collectionPath, type);
    }
    
    console.log(`Found ${imageFiles.length} images in collection ${collectionId}`);
    
    // Process images in batches
    const batchSize = 10;
    for (let i = 0; i < imageFiles.length; i += batchSize) {
      const batch = imageFiles.slice(i, i + batchSize);
      const processedImages = await Promise.all(
        batch.map(async (imageInfo) => {
          try {
            const thumbnailPath = await generateThumbnail(collectionId, imageInfo, type, collectionPath);
            return {
              collection_id: collectionId,
              filename: path.basename(imageInfo.path),
              relative_path: imageInfo.path,
              file_size: imageInfo.size,
              width: imageInfo.width,
              height: imageInfo.height,
              thumbnail_path: thumbnailPath
            };
          } catch (error) {
            console.error(`Error processing image ${imageInfo.path}:`, error);
            return null;
          }
        })
      );
      
      const validImages = processedImages.filter(img => img !== null);
      if (validImages.length > 0) {
        await db.addImages(validImages);
      }
    }
    
    console.log(`Completed scan for collection ${collectionId}`);
  } catch (error) {
    console.error(`Error scanning collection ${collectionId}:`, error);
  }
}

// Scan folder for images
async function scanFolder(folderPath) {
  const imageFiles = [];
  
  async function scanDirectory(dirPath, relativePath = '') {
    const items = await fs.readdir(dirPath, { withFileTypes: true });
    
    for (const item of items) {
      const fullPath = path.join(dirPath, item.name);
      const relativeItemPath = relativePath ? path.join(relativePath, item.name) : item.name;
      
      if (item.isDirectory()) {
        await scanDirectory(fullPath, relativeItemPath);
      } else if (item.isFile()) {
        const ext = path.extname(item.name).toLowerCase();
        if (SUPPORTED_FORMATS.includes(ext)) {
          const stats = await fs.stat(fullPath);
          imageFiles.push({
            path: relativeItemPath,
            fullPath,
            size: stats.size
          });
        }
      }
    }
  }
  
  await scanDirectory(folderPath);
  return imageFiles;
}

// Scan ZIP file for images
async function scanCompressedFile(filePath, type) {
  const imageFiles = [];
  
  try {
    switch (type) {
      case 'zip':
      case 'cbz':
        return await scanZip(filePath);
      
      case '7z':
        return await scan7z(filePath);
      
      case 'rar':
      case 'cbr':
        return await scanRar(filePath);
      
      case 'tar':
        return await scanTar(filePath);
      
      default:
        throw new Error(`Unsupported compressed file type: ${type}`);
    }
  } catch (error) {
    console.error(`Error scanning ${type} file ${filePath}:`, error);
    return [];
  }
}

async function scanZip(zipPath) {
  const imageFiles = [];
  
  return new Promise(async (resolve, reject) => {
    try {
      const zip = new StreamZip.async({ file: zipPath });
      
      const entries = await zip.entries();
      
      for (const [filename, entry] of Object.entries(entries)) {
        if (!entry.isDirectory) {
          const ext = path.extname(filename).toLowerCase();
          if (SUPPORTED_FORMATS.includes(ext)) {
            imageFiles.push({
              path: filename,
              fullPath: filename,
              size: entry.size
            });
          }
        }
      }
      
      await zip.close();
      resolve(imageFiles);
    } catch (error) {
      reject(error);
    }
  });
}

async function scan7z(filePath) {
  const imageFiles = [];
  
  try {
    const addon7z = require('node-7z');
    const stream = addon7z.list(filePath);
    
    return new Promise((resolve, reject) => {
      stream.on('data', (data) => {
        if (!data.isDirectory) {
          const ext = path.extname(data.file).toLowerCase();
          if (SUPPORTED_FORMATS.includes(ext)) {
            imageFiles.push({
              path: data.file,
              fullPath: data.file,
              size: data.size || 0
            });
          }
        }
      });
      
      stream.on('end', () => {
        resolve(imageFiles);
      });
      
      stream.on('error', (error) => {
        reject(error);
      });
    });
  } catch (error) {
    console.error('Error scanning 7z file:', error);
    return [];
  }
}

async function scanRar(filePath) {
  // For RAR files, we'll use a simple approach
  // Note: RAR support requires additional tools or libraries
  console.warn('RAR file scanning not fully implemented. Consider extracting to folder first.');
  return [];
}

async function scanTar(filePath) {
  // For TAR files, we'll use a simple approach
  // Note: TAR support requires additional tools or libraries
  console.warn('TAR file scanning not fully implemented. Consider extracting to folder first.');
  return [];
}

// Extract image from compressed file
async function extractImageFromCompressed(imageInfo, collectionType, collectionPath) {
  try {
    switch (collectionType) {
      case 'zip':
      case 'cbz':
        return await extractFromZip(imageInfo, collectionPath);
      
      case '7z':
        return await extractFrom7z(imageInfo, collectionPath);
      
      case 'rar':
      case 'cbr':
        console.warn('RAR extraction not fully implemented');
        return null;
      
      case 'tar':
        console.warn('TAR extraction not fully implemented');
        return null;
      
      default:
        throw new Error(`Unsupported compressed file type: ${collectionType}`);
    }
  } catch (error) {
    console.error(`Error extracting image from ${collectionType}:`, error);
    return null;
  }
}

async function extractFromZip(imageInfo, zipFilePath) {
  const zip = new StreamZip.async({ file: zipFilePath });
  const zipInstance = await zip;
  const imageBuffer = await zipInstance.entryData(imageInfo.path);
  await zipInstance.close();
  return imageBuffer;
}

async function extractFrom7z(imageInfo, zipFilePath) {
  const addon7z = require('node-7z');
  return new Promise((resolve, reject) => {
    const stream = addon7z.extract(zipFilePath, imageInfo.path);
    
    stream.on('data', (data) => {
      resolve(data);
    });
    
    stream.on('error', (error) => {
      reject(error);
    });
  });
}

// Generate thumbnail for image
async function generateThumbnail(collectionId, imageInfo, collectionType, collectionPath) {
  try {
    let imageBuffer;
    
    if (collectionType === 'folder') {
      imageBuffer = await fs.readFile(imageInfo.fullPath);
    } else if (['zip', 'cbz', '7z', 'rar', 'cbr', 'tar'].includes(collectionType)) {
      imageBuffer = await extractImageFromCompressed(imageInfo, collectionType, collectionPath);
    }
    
    // Get image dimensions first
    const metadata = await sharp(imageBuffer).metadata();
    imageInfo.width = metadata.width;
    imageInfo.height = metadata.height;
    
    // Use cache manager to generate thumbnail with distributed caching
    const thumbnailPath = await cacheManager.generateThumbnail(
      imageBuffer, // Pass buffer directly since we already have it
      collectionId,
      path.basename(imageInfo.path, path.extname(imageInfo.path)),
      { width: 300, height: 300, quality: 80 }
    );
    
    return thumbnailPath;
  } catch (error) {
    console.error(`Error generating thumbnail for ${imageInfo.path}:`, error);
    return null;
  }
}

// Serve collection thumbnail
router.get('/:id/thumbnail', async (req, res) => {
  try {
    const { id } = req.params;
    const collection = await db.getCollection(id);
    
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }

    // Check if thumbnail exists
    if (!collection.thumbnail_path || !await fs.pathExists(collection.thumbnail_path)) {
      return res.status(404).json({ error: 'Thumbnail not found' });
    }

    // Serve thumbnail file
    const thumbnailBuffer = await fs.readFile(collection.thumbnail_path);
    
    res.set({
      'Content-Type': 'image/jpeg',
      'Cache-Control': 'public, max-age=31536000', // 1 year cache
      'Content-Length': thumbnailBuffer.length
    });
    
    res.send(thumbnailBuffer);
  } catch (error) {
    console.error('Error serving collection thumbnail:', error);
    res.status(500).json({ error: 'Failed to serve thumbnail' });
  }
});

// Regenerate collection thumbnail
router.post('/:id/regenerate-thumbnail', async (req, res) => {
  try {
    const { id } = req.params;
    const collection = await db.getCollection(id);
    
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }

    // Generate new thumbnail
    const thumbnailPath = await collectionThumbnailService.regenerateCollectionThumbnail(id);
    
    if (thumbnailPath) {
      res.json({ 
        message: 'Thumbnail regenerated successfully',
        thumbnail_url: `/api/collections/${id}/thumbnail`
      });
    } else {
      res.status(400).json({ error: 'Failed to generate thumbnail' });
    }
  } catch (error) {
    console.error('Error regenerating collection thumbnail:', error);
    res.status(500).json({ error: 'Failed to regenerate thumbnail' });
  }
});

// Get random collection
router.get('/random', async (req, res) => {
  try {
    // Get total count of collections
    const totalCollections = await db.getCollectionCount();
    console.log('[RANDOM] Total collections:', totalCollections);
    
    if (totalCollections === 0) {
      return res.status(404).json({ error: 'No collections found' });
    }
    
    // Pick random index
    const randomIndex = Math.floor(Math.random() * totalCollections);
    console.log('[RANDOM] Random index:', randomIndex);
    
    // Get collection by index (skip randomIndex, limit 1)
    const collections = await db.getCollections({ skip: randomIndex, limit: 1 });
    console.log('[RANDOM] Collections found:', collections.length);
    
    if (collections.length === 0) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const randomCollection = collections[0];
    console.log('[RANDOM] Selected collection:', randomCollection.name);
    
    // Get statistics and tags for the random collection
    const [stats, tags] = await Promise.all([
      db.getCollectionStats(randomCollection.id),
      db.getCollectionTags(randomCollection.id)
    ]);
    
    const collectionWithStats = {
      ...randomCollection,
      statistics: stats || {
        view_count: 0,
        total_view_time: 0,
        search_count: 0,
        last_viewed: null,
        last_searched: null
      },
      tags: tags || []
    };
    
    res.json(collectionWithStats);
  } catch (error) {
    console.error('Error getting random collection:', error);
    res.status(500).json({ error: 'Failed to get random collection' });
  }
});

module.exports = router;
