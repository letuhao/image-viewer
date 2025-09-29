const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const StreamZip = require('node-stream-zip');
const sharp = require('sharp');
const db = require('../database');

// Get image by ID
router.get('/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const { collectionId } = req.query;
    
    if (!id || !collectionId) {
      return res.status(400).json({ error: 'Missing image ID or collection ID' });
    }
    
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const images = await db.getImages(collectionId);
    const image = images.find(img => img.id === parseInt(id));
    
    if (!image) {
      return res.status(404).json({ error: 'Image not found' });
    }
    
    res.json(image);
  } catch (error) {
    console.error('Error fetching image:', error);
    res.status(500).json({ error: 'Failed to fetch image' });
  }
});

// Get batch thumbnails for preloading
router.get('/:collectionId/batch-thumbnails', async (req, res) => {
  try {
    const { collectionId } = req.params;
    const { ids, width = 300, height = 300, quality = 80 } = req.query;
    
    if (!ids) {
      return res.status(400).json({ error: 'Image IDs are required' });
    }
    
    const imageIds = ids.split(',').map(id => parseInt(id.trim()));
    
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const images = await db.getImages(collectionId);
    const requestedImages = images.filter(img => imageIds.includes(img.id));
    
    const thumbnailPromises = requestedImages.map(async (image) => {
      try {
        const thumbnailPath = path.join(__dirname, '../cache/thumbnails', collectionId, `${path.basename(image.relative_path, path.extname(image.relative_path))}_thumb.jpg`);
        
        if (await fs.pathExists(thumbnailPath)) {
          const thumbnailBuffer = await fs.readFile(thumbnailPath);
          return {
            id: image.id,
            thumbnail: thumbnailBuffer.toString('base64'),
            filename: image.filename
          };
        }
        return null;
      } catch (error) {
        console.error(`Error loading thumbnail for image ${image.id}:`, error);
        return null;
      }
    });
    
    const thumbnails = (await Promise.all(thumbnailPromises)).filter(Boolean);
    
    res.json({
      thumbnails,
      requested: imageIds.length,
      found: thumbnails.length
    });
  } catch (error) {
    console.error('Error fetching batch thumbnails:', error);
    res.status(500).json({ error: 'Failed to fetch batch thumbnails' });
  }
});

// Serve image file
router.get('/:collectionId/:imageId/file', async (req, res) => {
  try {
    const { collectionId, imageId } = req.params;
    const { width, height, quality = 90 } = req.query;
    
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const images = await db.getImages(collectionId);
    const image = images.find(img => img.id === parseInt(imageId));
    
    if (!image) {
      return res.status(404).json({ error: 'Image not found' });
    }
    
    let imageBuffer;
    
    if (collection.type === 'folder') {
      const fullPath = path.join(collection.path, image.relative_path);
      imageBuffer = await fs.readFile(fullPath);
    } else if (['zip', 'cbz', '7z', 'rar', 'cbr', 'tar'].includes(collection.type)) {
      imageBuffer = await extractImageFromCompressed(collection.path, image.relative_path, collection.type);
    }
    
    // Process image if dimensions are specified
    if (width || height) {
      let sharpInstance = sharp(imageBuffer);
      
      if (width && height) {
        sharpInstance = sharpInstance.resize(parseInt(width), parseInt(height), { 
          fit: 'inside',
          withoutEnlargement: true 
        });
      } else if (width) {
        sharpInstance = sharpInstance.resize(parseInt(width), null, { 
          withoutEnlargement: true 
        });
      } else if (height) {
        sharpInstance = sharpInstance.resize(null, parseInt(height), { 
          withoutEnlargement: true 
        });
      }
      
      const format = sharpInstance.options.formatOut || 'jpeg';
      imageBuffer = await sharpInstance
        .jpeg({ quality: parseInt(quality) })
        .toBuffer();
    }
    
    // Set appropriate headers
    const ext = path.extname(image.filename).toLowerCase();
    const mimeTypes = {
      '.jpg': 'image/jpeg',
      '.jpeg': 'image/jpeg',
      '.png': 'image/png',
      '.gif': 'image/gif',
      '.webp': 'image/webp',
      '.bmp': 'image/bmp',
      '.tiff': 'image/tiff',
      '.svg': 'image/svg+xml'
    };
    
    res.set({
      'Content-Type': mimeTypes[ext] || 'application/octet-stream',
      'Cache-Control': 'public, max-age=31536000', // 1 year cache
      'Content-Length': imageBuffer.length
    });
    
    res.send(imageBuffer);
  } catch (error) {
    console.error('Error serving image:', error);
    res.status(500).json({ error: 'Failed to serve image' });
  }
});

// Serve thumbnail
router.get('/:collectionId/:imageId/thumbnail', async (req, res) => {
  try {
    const { collectionId, imageId } = req.params;
    
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const images = await db.getImages(collectionId);
    const image = images.find(img => img.id === parseInt(imageId));
    
    if (!image) {
      return res.status(404).json({ error: 'Image not found' });
    }
    
    if (!image.thumbnail_path || !await fs.pathExists(image.thumbnail_path)) {
      return res.status(404).json({ error: 'Thumbnail not found' });
    }
    
    const thumbnailBuffer = await fs.readFile(image.thumbnail_path);
    
    res.set({
      'Content-Type': 'image/jpeg',
      'Cache-Control': 'public, max-age=31536000',
      'Content-Length': thumbnailBuffer.length
    });
    
    res.send(thumbnailBuffer);
  } catch (error) {
    console.error('Error serving thumbnail:', error);
    res.status(500).json({ error: 'Failed to serve thumbnail' });
  }
});

// Get next/previous image
router.get('/:collectionId/:imageId/navigate', async (req, res) => {
  try {
    const { collectionId, imageId } = req.params;
    const { direction } = req.query; // 'next' or 'previous'
    
    if (!['next', 'previous'].includes(direction)) {
      return res.status(400).json({ error: 'Invalid direction' });
    }
    
    const images = await db.getImages(collectionId);
    const currentIndex = images.findIndex(img => img.id === parseInt(imageId));
    
    if (currentIndex === -1) {
      return res.status(404).json({ error: 'Current image not found' });
    }
    
    let targetIndex;
    if (direction === 'next') {
      targetIndex = (currentIndex + 1) % images.length;
    } else {
      targetIndex = (currentIndex - 1 + images.length) % images.length;
    }
    
    res.json(images[targetIndex]);
  } catch (error) {
    console.error('Error navigating images:', error);
    res.status(500).json({ error: 'Failed to navigate images' });
  }
});

// Get random image
router.get('/:collectionId/random', async (req, res) => {
  try {
    const { collectionId } = req.params;
    
    const images = await db.getImages(collectionId);
    if (images.length === 0) {
      return res.status(404).json({ error: 'No images found in collection' });
    }
    
    const randomIndex = Math.floor(Math.random() * images.length);
    res.json(images[randomIndex]);
  } catch (error) {
    console.error('Error getting random image:', error);
    res.status(500).json({ error: 'Failed to get random image' });
  }
});

// Search images
router.get('/:collectionId/search', async (req, res) => {
  try {
    const { collectionId } = req.params;
    const { query, page = 1, limit = 50 } = req.query;
    
    if (!query) {
      return res.status(400).json({ error: 'Search query is required' });
    }
    
    const images = await db.getImages(collectionId);
    const filteredImages = images.filter(img => 
      img.filename.toLowerCase().includes(query.toLowerCase())
    );
    
    // Track search (only for first page to avoid spam)
    if (parseInt(page) === 1) {
      await db.incrementSearchCount(collectionId);
    }
    
    const offset = (page - 1) * limit;
    const paginatedImages = filteredImages.slice(offset, offset + parseInt(limit));
    
    res.json({
      images: paginatedImages,
      pagination: {
        page: parseInt(page),
        limit: parseInt(limit),
        total: filteredImages.length,
        pages: Math.ceil(filteredImages.length / limit)
      }
    });
  } catch (error) {
    console.error('Error searching images:', error);
    res.status(500).json({ error: 'Failed to search images' });
  }
});

// Extract image from compressed file
async function extractImageFromCompressed(filePath, imagePath, collectionType) {
  try {
    switch (collectionType) {
      case 'zip':
      case 'cbz':
        return await extractFromZip(filePath, imagePath);
      
      case '7z':
        return await extractFrom7z(filePath, imagePath);
      
      case 'rar':
      case 'cbr':
        console.warn('RAR extraction not fully implemented');
        throw new Error('RAR extraction not supported yet');
      
      case 'tar':
        console.warn('TAR extraction not fully implemented');
        throw new Error('TAR extraction not supported yet');
      
      default:
        throw new Error(`Unsupported compressed file type: ${collectionType}`);
    }
  } catch (error) {
    console.error(`Error extracting image from ${collectionType}:`, error);
    throw error;
  }
}

async function extractFromZip(filePath, imagePath) {
  const zip = new StreamZip.async({ file: filePath });
  const zipInstance = await zip;
  const imageBuffer = await zipInstance.entryData(imagePath);
  await zipInstance.close();
  return imageBuffer;
}

async function extractFrom7z(filePath, imagePath) {
  const addon7z = require('node-7z');
  return new Promise((resolve, reject) => {
    const stream = addon7z.extract(filePath, imagePath);
    
    stream.on('data', (data) => {
      resolve(data);
    });
    
    stream.on('error', (error) => {
      reject(error);
    });
  });
}

module.exports = router;
