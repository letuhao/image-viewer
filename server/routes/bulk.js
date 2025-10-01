const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const db = require('../database');
const tagService = require('../services/tagService');
const collectionThumbnailService = require('../services/collectionThumbnailService');
const longPathHandler = require('../utils/longPathHandler');
const Logger = require('../utils/logger');

// Bulk add collections from a parent directory
router.post('/collections', async (req, res) => {
  const logger = new Logger('BulkCollections');
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false, autoAdd = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }
    
    // Check if parent path exists
    const exists = await longPathHandler.pathExistsSafe(parentPath);
    if (!exists) {
      return res.status(400).json({ error: 'Parent path does not exist' });
    }
    
    // Safety check for dangerous paths
    const dangerousSystemPaths = [
      'C:\\Windows', 'C:\\Program Files', 'C:\\Program Files (x86)', 
      'C:\\ProgramData', 'C:\\System Volume Information', 'C:\\$Recycle.Bin'
    ];
    
    const isDangerousParent = dangerousSystemPaths.some(dangerous => 
      parentPath.toLowerCase().startsWith(dangerous.toLowerCase())
    );
    
    if (isDangerousParent) {
      return res.status(400).json({ 
        error: 'Cannot scan system directories. Please choose a user directory or create a dedicated folder for your collections.' 
      });
    }
    
    // Get all potential collections (including subfolders if requested)
    logger.info('Finding collections in parent path', { parentPath });
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    logger.info('Found potential collections', { count: allCollections.length });
    
    const collections = [];
    const errors = [];
    
    for (let i = 0; i < allCollections.length; i++) {
      const collectionInfo = allCollections[i];
      try {
        logger.debug('Processing collection', { 
          index: i + 1, 
          total: allCollections.length, 
          name: collectionInfo.name 
        });
        
        // Check if collection already exists
        const existingCollections = await db.getCollections();
        const alreadyExists = existingCollections.some(col => 
          col.path === collectionInfo.path || col.name === collectionInfo.name
        );
        
        if (!alreadyExists) {
          // Generate metadata for the collection
          logger.debug('Generating metadata for collection', { name: collectionInfo.name });
          const metadata = await generateCollectionMetadata(collectionInfo.path, collectionInfo.type);
          logger.debug('Generated metadata', metadata);
          
          logger.debug('Adding collection to database', { name: collectionInfo.name });
          const collectionId = await db.addCollection(
            collectionInfo.name, 
            collectionInfo.path, 
            collectionInfo.type,
            metadata
          );
          
          collections.push({
            id: collectionId,
            name: collectionInfo.name,
            path: collectionInfo.path,
            type: collectionInfo.type,
            metadata
          });
          
          logger.info('Successfully added collection', { 
            name: collectionInfo.name, 
            collectionId 
          });
          
          // Generate collection thumbnail in background
          collectionThumbnailService.generateCollectionThumbnail(collectionId, collectionInfo.path, collectionInfo.type)
            .then(thumbnailPath => {
              if (thumbnailPath) {
                logger.info('Generated thumbnail for collection', { 
                  collectionId, 
                  thumbnailPath 
                });
              }
            })
            .catch(error => {
              logger.error('Failed to generate thumbnail for collection', { 
                collectionId, 
                error: error.message, 
                stack: error.stack 
              });
            });
        } else {
          logger.debug('Collection already exists, skipping', { name: collectionInfo.name });
        }
      } catch (error) {
        logger.error('Error processing collection', { 
          name: collectionInfo.name, 
          error: error.message, 
          stack: error.stack 
        });
        errors.push({
          item: collectionInfo.name,
          error: error.message
        });
      }
    }
    
    res.json({
      success: true,
      message: `Successfully added ${collections.length} collections`,
      collections,
      errors: errors.length > 0 ? errors : undefined,
      total: allCollections.length,
      added: collections.length,
      skipped: allCollections.length - collections.length - errors.length
    });
    
  } catch (error) {
    logger.error('Error in bulk add collections', { 
      error: error.message, 
      stack: error.stack 
    });
    res.status(500).json({ error: 'Failed to bulk add collections' });
  }
});

// Helper function to check if directory contains images
async function checkDirectoryHasImages(dirPath, maxDepth = 3, currentDepth = 0) {
  if (currentDepth >= maxDepth) {
    return false;
  }
  
  try {
    const items = await fs.readdir(dirPath, { withFileTypes: true });
    const supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.svg'];
    
    for (const item of items) {
      if (item.isFile()) {
        const ext = path.extname(item.name).toLowerCase();
        if (supportedFormats.includes(ext)) {
          return true;
        }
      } else if (item.isDirectory() && currentDepth < maxDepth - 1) {
        // Recursively check subdirectories
        const hasImages = await checkDirectoryHasImages(
          path.join(dirPath, item.name), 
          maxDepth, 
          currentDepth + 1
        );
        if (hasImages) {
          return true;
        }
      }
    }
    return false;
  } catch (error) {
    return false;
  }
}

// Get preview of what would be added (without actually adding)
router.post('/preview', async (req, res) => {
  const logger = new Logger('BulkPreview');
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }
    
    // Check if parent path exists
    const exists = await longPathHandler.pathExistsSafe(parentPath);
    if (!exists) {
      return res.status(400).json({ error: 'Parent path does not exist' });
    }
    
    // Safety check for dangerous paths
    const dangerousSystemPaths = [
      'C:\\Windows', 'C:\\Program Files', 'C:\\Program Files (x86)', 
      'C:\\ProgramData', 'C:\\System Volume Information', 'C:\\$Recycle.Bin'
    ];
    
    const isDangerousParent = dangerousSystemPaths.some(dangerous => 
      parentPath.toLowerCase().startsWith(dangerous.toLowerCase())
    );
    
    if (isDangerousParent) {
      return res.status(400).json({ 
        error: 'Cannot scan system directories. Please choose a user directory or create a dedicated folder for your collections.' 
      });
    }
    
    // Get all potential collections (including subfolders if requested)
    logger.info('Finding collections for preview', { parentPath });
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    logger.info('Found potential collections for preview', { count: allCollections.length });
    
    const potentialCollections = [];
    const errors = [];
    
    for (let i = 0; i < allCollections.length; i++) {
      const collectionInfo = allCollections[i];
      try {
        let imageCount = 0;
        let size = 'Directory';
        
        if (collectionInfo.type === 'folder') {
          // Count images in directory
          imageCount = await countImagesInDirectory(collectionInfo.path);
        } else if (['zip', '7z', 'rar', 'tar'].includes(collectionInfo.type)) {
          // For compressed files, we can't easily count images without extracting
          imageCount = `Unknown (${collectionInfo.type.toUpperCase()} file)`;
          const stats = await fs.stat(collectionInfo.path);
          size = stats.size;
        }
        
        // Check if collection already exists
        const existingCollections = await db.getCollections();
        const alreadyExists = existingCollections.some(col => 
          col.path === collectionInfo.path || col.name === collectionInfo.name
        );
        
        // Generate preview metadata
        const metadata = await generateCollectionMetadata(collectionInfo.path, collectionInfo.type);
        
        potentialCollections.push({
          name: collectionInfo.name,
          path: collectionInfo.path,
          type: collectionInfo.type,
          imageCount,
          alreadyExists,
          size,
          metadata
        });
      } catch (error) {
        errors.push({
          item: collectionInfo.name,
          error: error.message
        });
      }
    }
    
    res.json({
      success: true,
      potentialCollections,
      errors: errors.length > 0 ? errors : undefined,
      total: potentialCollections.length,
      valid: potentialCollections.length,
      existing: potentialCollections.filter(col => col.alreadyExists).length
    });
    
  } catch (error) {
    logger.error('Error in bulk preview', { 
      error: error.message, 
      stack: error.stack 
    });
    res.status(500).json({ error: 'Failed to preview collections' });
  }
});

// Helper function to count images in a directory
async function countImagesInDirectory(dirPath, maxDepth = 3, currentDepth = 0) {
  if (currentDepth >= maxDepth) {
    return 0;
  }
  
  try {
    const items = await fs.readdir(dirPath, { withFileTypes: true });
    const supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.svg'];
    let count = 0;
    
    for (const item of items) {
      if (item.isFile()) {
        const ext = path.extname(item.name).toLowerCase();
        if (supportedFormats.includes(ext)) {
          count++;
        }
      } else if (item.isDirectory() && currentDepth < maxDepth - 1) {
        // Recursively count in subdirectories
        count += await countImagesInDirectory(
          path.join(dirPath, item.name), 
          maxDepth, 
          currentDepth + 1
        );
      }
    }
    return count;
  } catch (error) {
    return 0;
  }
}

// Find all collections in a directory (including subfolders if requested)
async function findAllCollections(parentPath, includeSubfolders = false, collectionPrefix = '') {
  const collections = [];
  const supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.svg'];
  const zipFormats = ['.zip', '.cbz', '.cbr', '.7z', '.rar', '.tar', '.tar.gz', '.tar.bz2'];
  
  // Determine collection type based on file extension
  function getCollectionType(filePath) {
    const ext = path.extname(filePath).toLowerCase();
    
    switch (ext) {
      case '.zip':
      case '.cbz':
        return 'zip';
      case '.7z':
        return '7z';
      case '.rar':
      case '.cbr':
        return 'rar';
      case '.tar':
      case '.tar.gz':
      case '.tar.bz2':
        return 'tar';
      default:
        return 'zip'; // fallback
    }
  }
  
  // Common sensitive system directories to avoid
  const dangerousPaths = [
    'C:\\Windows',
    'C:\\Program Files',
    'C:\\Program Files (x86)',
    'C:\\ProgramData',
    'C:\\System Volume Information',
    'C:\\$Recycle.Bin',
    'C:\\Recovery',
    'C:\\Boot',
    'C:\\EFI',
    'C:\\System32',
    'C:\\SysWOW64',
    'C:\\Windows.old'
  ];
  
  // Check if path is dangerous or sensitive
  function isDangerousPath(dirPath) {
    const normalizedPath = path.normalize(dirPath).toLowerCase();
    
    // Check against known dangerous paths
    if (dangerousPaths.some(dangerous => 
      normalizedPath.startsWith(dangerous.toLowerCase()) || 
      normalizedPath === dangerous.toLowerCase()
    )) {
      return true;
    }
    
    // Additional safety checks
    // Skip if path contains common sensitive patterns
    const sensitivePatterns = [
      '\\appdata\\local\\temp',
      '\\appdata\\local\\microsoft\\windows\\temporary internet files',
      '\\appdata\\roaming\\microsoft\\windows\\recent',
      '\\appdata\\local\\microsoft\\windows\\history',
      '\\appdata\\local\\microsoft\\windows\\cookies',
      '\\temp\\',
      '\\tmp\\',
      '\\cache\\',
      '\\logs\\',
      '\\log\\'
    ];
    
    return sensitivePatterns.some(pattern => normalizedPath.includes(pattern));
  }
  
  async function scanDirectory(dirPath, currentDepth = 0, maxDepth = includeSubfolders ? 10 : 1) {
    if (currentDepth >= maxDepth) {
      return;
    }
    
    // Skip dangerous system directories
    if (isDangerousPath(dirPath)) {
      // Skip dangerous system directories silently
      return;
    }
    
    try {
      const items = await longPathHandler.readDirSafe(dirPath, { withFileTypes: true });
      
      for (const item of items) {
        const fullPath = longPathHandler.joinSafe(dirPath, item.name);
        const relativePath = path.relative(parentPath, fullPath);
        
        // Skip hidden/system files and directories
        if (item.name.startsWith('.') || item.name.startsWith('$')) {
          continue;
        }
        
        if (item.isDirectory()) {
          // Skip dangerous subdirectories
          if (isDangerousPath(fullPath)) {
            continue;
          }
          
          // Check if directory contains images
          const hasImages = await checkDirectoryHasImages(fullPath);
          if (hasImages) {
            // Use relative path for nested collections, or just name for top-level
            const collectionName = includeSubfolders && currentDepth > 0 
              ? collectionPrefix + relativePath.replace(/\\/g, ' - ') 
              : collectionPrefix + item.name;
            
            collections.push({
              name: collectionName,
              path: fullPath,
              type: 'folder'
            });
          }
          
          // Recursively scan subdirectories if enabled
          if (includeSubfolders && currentDepth < maxDepth - 1) {
            await scanDirectory(fullPath, currentDepth + 1, maxDepth);
          }
        } else if (item.isFile()) {
          // Check if file is a ZIP archive
          const ext = path.extname(item.name).toLowerCase();
          if (zipFormats.includes(ext)) {
            // Use relative path for nested collections, or just name for top-level
            const collectionName = includeSubfolders && currentDepth > 0 
              ? collectionPrefix + relativePath.replace(/\\/g, ' - ') 
              : collectionPrefix + item.name;
            
            collections.push({
              name: collectionName,
              path: fullPath,
              type: getCollectionType(fullPath)
            });
          }
        }
      }
    } catch (error) {
      // Only log errors for non-permission issues to reduce noise
      if (!error.message.includes('EPERM') && !error.message.includes('EACCES')) {
        // Skip permission errors silently
      }
    }
  }
  
  await scanDirectory(parentPath);
  return collections;
}

// Generate metadata for a collection
async function generateCollectionMetadata(collectionPath, collectionType) {
  const logger = new Logger('MetadataGenerator');
  logger.debug('Generating metadata for collection', { collectionPath, collectionType });
  
  const metadata = {
    created_at: new Date().toISOString(),
    last_scanned: new Date().toISOString(),
    total_images: 0,
    total_size: 0,
    image_formats: [],
    has_subfolders: false,
    average_image_size: 0,
    collection_stats: {},
    auto_tags: []
  };
  
  try {
    if (collectionType === 'folder') {
      logger.debug('Getting directory stats', { collectionPath });
      const stats = await getDirectoryStats(collectionPath);
      metadata.total_images = stats.totalImages;
      metadata.total_size = stats.totalSize;
      metadata.image_formats = stats.formats;
      metadata.has_subfolders = stats.hasSubfolders;
      metadata.average_image_size = stats.totalImages > 0 ? Math.round(stats.totalSize / stats.totalImages) : 0;
      metadata.collection_stats = {
        directories_scanned: stats.directoriesScanned,
        max_depth: stats.maxDepth
      };
    } else if (collectionType === 'zip') {
      const stats = await fs.stat(collectionPath);
      metadata.total_size = stats.size;
      metadata.collection_stats = {
        zip_size: stats.size,
        zip_modified: stats.mtime.toISOString()
      };
    }
    
    // Auto-generate tags based on collection name
    const collectionName = path.basename(collectionPath, path.extname(collectionPath));
    logger.debug('Auto-tagging collection', { collectionName });
    const autoTags = tagService.autoTagFromName(collectionName);
    logger.debug('Generated auto-tags', { autoTags });
    metadata.auto_tags = autoTags;
    
  } catch (error) {
    logger.error('Error generating metadata for collection', { 
      collectionPath, 
      error: error.message, 
      stack: error.stack 
    });
  }
  
  return metadata;
}

// Get directory statistics
async function getDirectoryStats(dirPath, maxDepth = 5, currentDepth = 0) {
  const stats = {
    totalImages: 0,
    totalSize: 0,
    formats: new Set(),
    hasSubfolders: false,
    directoriesScanned: 0,
    maxDepth: 0
  };
  
  const supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.tiff', '.svg'];
  
  async function scanDirectory(currentPath, depth = 0) {
    if (depth >= maxDepth) return;
    
    try {
      stats.directoriesScanned++;
      stats.maxDepth = Math.max(stats.maxDepth, depth);
      
      const items = await fs.readdir(currentPath, { withFileTypes: true });
      
      for (const item of items) {
        const fullPath = path.join(currentPath, item.name);
        
        if (item.isFile()) {
          const ext = path.extname(item.name).toLowerCase();
          if (supportedFormats.includes(ext)) {
            stats.totalImages++;
            stats.formats.add(ext);
            
            try {
              const fileStats = await longPathHandler.statSafe(fullPath);
              stats.totalSize += fileStats.size;
            } catch (error) {
              // Ignore files that can't be stat'd
            }
          }
        } else if (item.isDirectory()) {
          stats.hasSubfolders = true;
          await scanDirectory(fullPath, depth + 1);
        }
      }
    } catch (error) {
      // Ignore directories that can't be read
    }
  }
  
  await scanDirectory(dirPath);
  
  // Convert Set to Array for JSON serialization
  stats.formats = Array.from(stats.formats);
  
  return stats;
}

// Find all potential collections in a parent directory
async function findAllCollections(parentPath, includeSubfolders = false, collectionPrefix = '') {
  const collections = [];
  const supportedFormats = ['.zip', '.cbz', '.7z', '.rar', '.cbr', '.tar', '.tar.gz', '.tar.bz2'];
  const imageFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp'];
  
  const dangerousPaths = [
    'C:\\Windows', 'C:\\Program Files', 'C:\\Program Files (x86)', 
    'C:\\ProgramData', 'C:\\System Volume Information', 'C:\\$Recycle.Bin',
    'C:\\Users\\All Users', 'C:\\Users\\Default'
  ];
  
  const isDangerousPath = (path) => {
    return dangerousPaths.some(dangerous => 
      path.toLowerCase().startsWith(dangerous.toLowerCase())
    );
  };
  
  const isHiddenOrSystem = (name) => {
    return name.startsWith('.') || name.startsWith('$') || name.startsWith('~');
  };
  
  async function scanDirectory(dirPath, depth = 0) {
    if (depth > 10) return; // Prevent infinite recursion
    
    try {
      const items = await longPathHandler.readDirSafe(dirPath, { withFileTypes: true });
      
      for (const item of items) {
        const fullPath = longPathHandler.joinSafe(dirPath, item.name);
        
        // Skip dangerous, hidden, or system paths
        if (isDangerousPath(fullPath) || isHiddenOrSystem(item.name)) {
          continue;
        }
        
        if (item.isDirectory()) {
          // Check if directory contains images (potential collection)
          const hasImages = await checkDirectoryForImages(fullPath);
          if (hasImages) {
            const collectionName = collectionPrefix + item.name;
            collections.push({
              name: collectionName,
              path: fullPath,
              type: 'folder'
            });
          }
          
          // Recursively scan subdirectories if requested
          if (includeSubfolders) {
            await scanDirectory(fullPath, depth + 1);
          }
        } else if (item.isFile()) {
          // Check if file is a supported compressed format
          const ext = path.extname(item.name).toLowerCase();
          if (supportedFormats.includes(ext)) {
            const collectionName = collectionPrefix + path.basename(item.name, ext);
            const collectionType = getCollectionType(ext);
            collections.push({
              name: collectionName,
              path: fullPath,
              type: collectionType
            });
          }
        }
      }
    } catch (error) {
      // Skip directories that can't be read (permission issues, etc.)
      // Skip directory scan errors silently
    }
  }
  
  // Check if directory contains images
  async function checkDirectoryForImages(dirPath) {
    try {
      const items = await longPathHandler.readDirSafe(dirPath, { withFileTypes: true });
      return items.some(item => {
        if (item.isFile()) {
          const ext = path.extname(item.name).toLowerCase();
          return imageFormats.includes(ext);
        }
        return false;
      });
    } catch (error) {
      return false;
    }
  }
  
  // Get collection type from file extension
  function getCollectionType(ext) {
    const typeMap = {
      '.zip': 'zip',
      '.cbz': 'zip',
      '.7z': '7z',
      '.rar': 'rar',
      '.cbr': 'rar',
      '.tar': 'tar',
      '.tar.gz': 'tar',
      '.tar.bz2': 'tar'
    };
    return typeMap[ext] || 'zip';
  }
  
  await scanDirectory(parentPath);
  return collections;
}

// Generate collection metadata (duplicate function - this one appears to be unused)
async function generateCollectionMetadata(collectionPath, collectionType) {
  const logger = new Logger('MetadataGenerator');
  logger.debug('Generating metadata for collection', { collectionPath, collectionType });
  
  const metadata = {
    created_at: new Date().toISOString(),
    last_scanned: new Date().toISOString(),
    total_images: 0,
    total_size: 0,
    image_formats: [],
    has_subfolders: false,
    average_image_size: 0
  };
  
  const supportedFormats = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp'];
  
  if (collectionType === 'folder') {
    logger.debug('Getting directory stats', { collectionPath });
    
    let totalImages = 0;
    let totalSize = 0;
    const formats = new Set();
    let hasSubfolders = false;
    
    async function scanDirectory(dirPath, depth = 0) {
      if (depth > 5) return; // Prevent deep recursion
      
      try {
        const items = await longPathHandler.readDirSafe(dirPath, { withFileTypes: true });
        
        for (const item of items) {
          const fullPath = longPathHandler.joinSafe(dirPath, item.name);
          
          if (item.isFile()) {
            const ext = path.extname(item.name).toLowerCase();
            if (supportedFormats.includes(ext)) {
              totalImages++;
              formats.add(ext);
              
              try {
                const stats = await fs.stat(fullPath);
                totalSize += stats.size;
              } catch (error) {
                // Ignore files that can't be stat'd
              }
            }
          } else if (item.isDirectory()) {
            hasSubfolders = true;
            await scanDirectory(fullPath, depth + 1);
          }
        }
      } catch (error) {
        // Ignore directories that can't be read
      }
    }
    
    await scanDirectory(collectionPath);
    
    metadata.total_images = totalImages;
    metadata.total_size = totalSize;
    metadata.image_formats = Array.from(formats);
    metadata.has_subfolders = hasSubfolders;
    metadata.average_image_size = totalImages > 0 ? Math.round(totalSize / totalImages) : 0;
    metadata.collection_stats = {
      directories_scanned: 1,
      max_depth: 0
    };
  } else {
    // For compressed files, get file stats
    try {
      const stats = await fs.stat(collectionPath);
      metadata.total_size = stats.size;
      metadata.collection_stats = {
        zip_size: stats.size,
        zip_modified: stats.mtime.toISOString()
      };
    } catch (error) {
      logger.error('Error getting stats for collection', { 
        collectionPath, 
        error: error.message, 
        stack: error.stack 
      });
    }
  }
  
  // Auto-tagging based on collection name
  const collectionName = path.basename(collectionPath, path.extname(collectionPath));
  logger.debug('Auto-tagging collection', { collectionName });
  const autoTags = tagService.autoTagFromName(collectionName);
  logger.debug('Generated auto-tags', { autoTags });
  metadata.auto_tags = autoTags;
  
  return metadata;
}

module.exports = router;
module.exports.findAllCollections = findAllCollections;
module.exports.generateCollectionMetadata = generateCollectionMetadata;
