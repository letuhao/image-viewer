const express = require('express');
const router = express.Router();
const fs = require('fs-extra');
const path = require('path');
const db = require('../database');
const tagService = require('../services/tagService');

// Bulk add collections from a parent directory
router.post('/collections', async (req, res) => {
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false, autoAdd = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }
    
    // Check if parent path exists
    const exists = await fs.pathExists(parentPath);
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
    console.log(`[BULK ADD] Finding collections in: ${parentPath}`);
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    console.log(`[BULK ADD] Found ${allCollections.length} potential collections`);
    
    const collections = [];
    const errors = [];
    
    for (let i = 0; i < allCollections.length; i++) {
      const collectionInfo = allCollections[i];
      try {
        console.log(`[BULK ADD] Processing ${i + 1}/${allCollections.length}: ${collectionInfo.name}`);
        
        // Check if collection already exists
        const existingCollections = await db.getCollections();
        const alreadyExists = existingCollections.some(col => 
          col.path === collectionInfo.path || col.name === collectionInfo.name
        );
        
        if (!alreadyExists) {
          // Generate metadata for the collection
          console.log(`[BULK ADD] Generating metadata for: ${collectionInfo.name}`);
          const metadata = await generateCollectionMetadata(collectionInfo.path, collectionInfo.type);
          console.log(`[BULK ADD] Generated metadata:`, JSON.stringify(metadata, null, 2));
          
          console.log(`[BULK ADD] Adding collection to database: ${collectionInfo.name}`);
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
          
          console.log(`[BULK ADD] Successfully added collection: ${collectionInfo.name} (ID: ${collectionId})`);
        } else {
          console.log(`[BULK ADD] Collection already exists, skipping: ${collectionInfo.name}`);
        }
      } catch (error) {
        console.error(`[BULK ADD] Error processing collection ${collectionInfo.name}:`, error);
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
    console.error('Error in bulk add collections:', error);
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
  try {
    const { parentPath, collectionPrefix = '', includeSubfolders = false } = req.body;
    
    if (!parentPath) {
      return res.status(400).json({ error: 'Parent path is required' });
    }
    
    // Check if parent path exists
    const exists = await fs.pathExists(parentPath);
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
    console.log(`[BULK PREVIEW] Finding collections in: ${parentPath}`);
    const allCollections = await findAllCollections(parentPath, includeSubfolders, collectionPrefix);
    console.log(`[BULK PREVIEW] Found ${allCollections.length} potential collections`);
    
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
    console.error('Error in bulk preview:', error);
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
      console.log(`Skipping dangerous system directory: ${dirPath}`);
      return;
    }
    
    try {
      const items = await fs.readdir(dirPath, { withFileTypes: true });
      
      for (const item of items) {
        const fullPath = path.join(dirPath, item.name);
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
        console.error(`Error scanning directory ${dirPath}:`, error.message);
      }
    }
  }
  
  await scanDirectory(parentPath);
  return collections;
}

// Generate metadata for a collection
async function generateCollectionMetadata(collectionPath, collectionType) {
  console.log(`[METADATA] Generating metadata for: ${collectionPath} (type: ${collectionType})`);
  
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
      console.log(`[METADATA] Getting directory stats for: ${collectionPath}`);
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
    console.log(`[METADATA] Auto-tagging collection: ${collectionName}`);
    const autoTags = tagService.autoTagFromName(collectionName);
    console.log(`[METADATA] Generated auto-tags:`, autoTags);
    metadata.auto_tags = autoTags;
    
  } catch (error) {
    console.error(`Error generating metadata for ${collectionPath}:`, error);
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
              const fileStats = await fs.stat(fullPath);
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

module.exports = router;
