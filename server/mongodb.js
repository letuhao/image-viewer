const { MongoClient, ObjectId } = require('mongodb');

class MongoDBDatabase {
  constructor() {
    this.client = null;
    this.db = null;
    this.collections = null;
    this.images = null;
    this.cache = null;
    this.collectionStats = null;
    this.collectionTags = null;
    this.collectionSessions = null;
  }

  async connect() {
    try {
      // Use local MongoDB by default, but allow environment variable override
      const mongoUrl = process.env.MONGODB_URL || 'mongodb://localhost:27017';
      const dbName = process.env.MONGODB_DB_NAME || 'image_viewer';
      
      console.log(`[MONGODB] Connecting to ${mongoUrl}/${dbName}`);
      
      this.client = new MongoClient(mongoUrl);
      await this.client.connect();
      
      this.db = this.client.db(dbName);
      
      // Get collection references
      this.collections = this.db.collection('collections');
      this.images = this.db.collection('images');
      this.cache = this.db.collection('cache');
      this.collectionStats = this.db.collection('collection_stats');
      this.collectionTags = this.db.collection('collection_tags');
      this.collectionSessions = this.db.collection('collection_sessions');
      this.cacheFolders = this.db.collection('cache_folders');
      this.collectionCacheBindings = this.db.collection('collection_cache_bindings');
      
      await this.createIndexes();
      console.log('[MONGODB] Connected successfully');
    } catch (error) {
      console.error('[MONGODB] Connection failed:', error);
      throw error;
    }
  }

  async createIndexes() {
    try {
      // Collections indexes
      await this.collections.createIndex({ name: 1 });
      await this.collections.createIndex({ path: 1 }, { unique: true });
      await this.collections.createIndex({ type: 1 });
      await this.collections.createIndex({ created_at: -1 });

      // Images indexes
      await this.images.createIndex({ collection_id: 1 });
      await this.images.createIndex({ filename: 1 });
      await this.images.createIndex({ collection_id: 1, filename: 1 });

      // Cache indexes
      await this.cache.createIndex({ key: 1 }, { unique: true });
      await this.cache.createIndex({ expires_at: 1 }, { expireAfterSeconds: 0 });

      // Collection stats indexes
      await this.collectionStats.createIndex({ collection_id: 1 }, { unique: true });
      await this.collectionStats.createIndex({ view_count: -1 });
      await this.collectionStats.createIndex({ total_view_time: -1 });

      // Collection tags indexes
      await this.collectionTags.createIndex({ collection_id: 1 });
      await this.collectionTags.createIndex({ tag: 1 });
      await this.collectionTags.createIndex({ collection_id: 1, tag: 1, added_by: 1 }, { unique: true });

      // Collection sessions indexes
      await this.collectionSessions.createIndex({ collection_id: 1 });
      await this.collectionSessions.createIndex({ session_id: 1 });
      await this.collectionSessions.createIndex({ start_time: -1 });

    // Cache folders indexes
    await this.cacheFolders.createIndex({ name: 1 }); // Remove unique constraint on name
    await this.cacheFolders.createIndex({ path: 1 }, { unique: true }); // Keep unique constraint on path
    await this.cacheFolders.createIndex({ is_active: 1 });
    await this.cacheFolders.createIndex({ priority: 1 });

      // Collection cache bindings indexes
      await this.collectionCacheBindings.createIndex({ collection_id: 1 }, { unique: true });
      await this.collectionCacheBindings.createIndex({ cache_folder_id: 1 });
      await this.collectionCacheBindings.createIndex({ created_at: -1 });

      console.log('[MONGODB] Indexes created successfully');
    } catch (error) {
      console.error('[MONGODB] Error creating indexes:', error);
      throw error;
    }
  }

  // Collection methods
  async addCollection(name, path, type, metadata = {}) {
    try {
      const collectionData = {
        name,
        path,
        type,
        created_at: new Date(),
        updated_at: new Date(),
        settings: metadata || {}
      };

      const result = await this.collections.insertOne(collectionData);
      const collectionId = result.insertedId;

      // Initialize collection stats
      await this.initializeCollectionStats(collectionId);

      // Auto-add tags from metadata if they exist
      if (metadata.auto_tags && Array.isArray(metadata.auto_tags) && metadata.auto_tags.length > 0) {
        for (const tag of metadata.auto_tags) {
          await this.addTagToCollection(collectionId, tag, 'system');
        }
      }

      // Assign cache folder to collection
      try {
        await this.getCacheFolderForCollection(collectionId);
        console.log(`[CACHE] Assigned cache folder to collection ${collectionId}`);
      } catch (error) {
        console.error(`[CACHE] Failed to assign cache folder to collection ${collectionId}:`, error);
      }

      return collectionId;
    } catch (error) {
      if (error.code === 11000) {
        throw new Error('Collection with this path already exists');
      }
      throw error;
    }
  }

  async getCollection(id) {
    try {
      const objectId = typeof id === 'string' ? new ObjectId(id) : id;
      return await this.collections.findOne({ _id: objectId });
    } catch (error) {
      return null;
    }
  }

  async getCollectionByPath(path) {
    return await this.collections.findOne({ path });
  }

  async getAllCollections() {
    const collections = await this.collections.find({}).sort({ created_at: -1 }).toArray();
    
    // Populate statistics and tags for each collection
    for (const collection of collections) {
      collection.statistics = await this.getCollectionStats(collection._id);
      collection.tags = await this.getCollectionTags(collection._id);
    }
    
    return collections;
  }

  async updateCollection(id, updates) {
    try {
      const objectId = typeof id === 'string' ? new ObjectId(id) : id;
      updates.updated_at = new Date();
      
      const result = await this.collections.updateOne(
        { _id: objectId },
        { $set: updates }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async deleteCollection(id) {
    try {
      const objectId = typeof id === 'string' ? new ObjectId(id) : id;
      
      // Delete related data
      await Promise.all([
        this.images.deleteMany({ collection_id: objectId }),
        this.collectionStats.deleteOne({ collection_id: objectId }),
        this.collectionTags.deleteMany({ collection_id: objectId }),
        this.collectionSessions.deleteMany({ collection_id: objectId })
      ]);
      
      // Delete collection
      const result = await this.collections.deleteOne({ _id: objectId });
      return result.deletedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  // Image methods
  async addImage(collectionId, imageData) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const image = {
        collection_id: objectId,
        filename: imageData.filename,
        relative_path: imageData.relative_path,
        file_size: imageData.file_size || null,
        width: imageData.width || null,
        height: imageData.height || null,
        thumbnail_path: imageData.thumbnail_path || null,
        created_at: new Date()
      };

      const result = await this.images.insertOne(image);
      return result.insertedId;
    } catch (error) {
      throw error;
    }
  }

  async getImages(collectionId, options = {}) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      let query = { collection_id: objectId };
      let cursor = this.images.find(query).sort({ filename: 1 });

      if (options.limit) {
        cursor = cursor.limit(options.limit);
      }

      if (options.offset) {
        cursor = cursor.skip(options.offset);
      }

      return await cursor.toArray();
    } catch (error) {
      throw error;
    }
  }

  async getImage(collectionId, imageId) {
    try {
      const collectionObjectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      const imageObjectId = typeof imageId === 'string' ? new ObjectId(imageId) : imageId;
      
      return await this.images.findOne({ 
        _id: imageObjectId, 
        collection_id: collectionObjectId 
      });
    } catch (error) {
      return null;
    }
  }

  async deleteImage(collectionId, imageId) {
    try {
      const collectionObjectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      const imageObjectId = typeof imageId === 'string' ? new ObjectId(imageId) : imageId;
      
      const result = await this.images.deleteOne({ 
        _id: imageObjectId, 
        collection_id: collectionObjectId 
      });
      
      return result.deletedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async deleteCollectionImages(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      const result = await this.images.deleteMany({ collection_id: objectId });
      return result.deletedCount;
    } catch (error) {
      throw error;
    }
  }

  // Cache methods
  async setCache(key, value, ttl = null) {
    try {
      const expiresAt = ttl ? new Date(Date.now() + ttl * 1000) : null;
      
      const cacheData = {
        key,
        value,
        expires_at: expiresAt,
        created_at: new Date()
      };

      await this.cache.replaceOne(
        { key },
        cacheData,
        { upsert: true }
      );
    } catch (error) {
      throw error;
    }
  }

  async getCache(key) {
    try {
      const result = await this.cache.findOne({ key });
      return result ? result.value : null;
    } catch (error) {
      return null;
    }
  }

  async deleteCache(key) {
    try {
      const result = await this.cache.deleteOne({ key });
      return result.deletedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  // Collection statistics methods
  async initializeCollectionStats(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const statsData = {
        collection_id: objectId,
        view_count: 0,
        total_view_time: 0,
        search_count: 0,
        last_viewed: null,
        last_searched: null,
        created_at: new Date(),
        updated_at: new Date()
      };

      await this.collectionStats.replaceOne(
        { collection_id: objectId },
        statsData,
        { upsert: true }
      );
    } catch (error) {
      throw error;
    }
  }

  async getCollectionStats(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      return await this.collectionStats.findOne({ collection_id: objectId });
    } catch (error) {
      return null;
    }
  }

  async incrementViewCount(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const result = await this.collectionStats.updateOne(
        { collection_id: objectId },
        { 
          $inc: { view_count: 1 },
          $set: { 
            last_viewed: new Date(),
            updated_at: new Date()
          }
        }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async incrementSearchCount(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const result = await this.collectionStats.updateOne(
        { collection_id: objectId },
        { 
          $inc: { search_count: 1 },
          $set: { 
            last_searched: new Date(),
            updated_at: new Date()
          }
        }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async addViewTime(collectionId, timeInSeconds) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const result = await this.collectionStats.updateOne(
        { collection_id: objectId },
        { 
          $inc: { total_view_time: timeInSeconds },
          $set: { 
            last_viewed: new Date(),
            updated_at: new Date()
          }
        }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async startViewSession(collectionId, sessionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const sessionData = {
        collection_id: objectId,
        session_id,
        start_time: new Date(),
        end_time: null,
        total_time: 0
      };

      const result = await this.collectionSessions.insertOne(sessionData);
      return result.insertedId;
    } catch (error) {
      throw error;
    }
  }

  async endViewSession(sessionId, totalTime) {
    try {
      const result = await this.collectionSessions.updateOne(
        { session_id: sessionId },
        { 
          $set: { 
            end_time: new Date(),
            total_time: totalTime
          }
        }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  // Collection tags methods
  async getCollectionTags(collectionId) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const tags = await this.collectionTags.find({ collection_id: objectId }).toArray();
      
      // Group tags by tag name and count occurrences
      const tagMap = new Map();
      tags.forEach(tag => {
        const tagName = tag.tag;
        if (tagMap.has(tagName)) {
          const existing = tagMap.get(tagName);
          existing.count++;
          existing.added_by_list.push(tag.added_by);
        } else {
          tagMap.set(tagName, {
            tag: tagName,
            count: 1,
            added_by_list: [tag.added_by]
          });
        }
      });

      return Array.from(tagMap.values());
    } catch (error) {
      return [];
    }
  }

  async addTagToCollection(collectionId, tag, addedBy = 'anonymous') {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const tagData = {
        collection_id: objectId,
        tag,
        added_by: addedBy,
        added_at: new Date()
      };

      await this.collectionTags.replaceOne(
        { collection_id: objectId, tag, added_by: addedBy },
        tagData,
        { upsert: true }
      );
      
      return true;
    } catch (error) {
      throw error;
    }
  }

  async removeTagFromCollection(collectionId, tag, addedBy = null) {
    try {
      const objectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const query = { collection_id: objectId, tag };
      if (addedBy) {
        query.added_by = addedBy;
      }

      const result = await this.collectionTags.deleteMany(query);
      return result.deletedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  async searchTags(query, limit = 10) {
    try {
      const regex = new RegExp(query, 'i');
      
      const tags = await this.collectionTags.aggregate([
        { $match: { tag: regex } },
        { $group: { _id: '$tag', count: { $sum: 1 } } },
        { $sort: { count: -1 } },
        { $limit: limit }
      ]).toArray();

      return tags.map(tag => ({
        tag: tag._id,
        count: tag.count
      }));
    } catch (error) {
      return [];
    }
  }

  async getTagSuggestions(query, limit = 10) {
    try {
      const regex = new RegExp(query, 'i');
      
      const tags = await this.collectionTags.aggregate([
        { $match: { tag: regex } },
        { $group: { _id: '$tag', count: { $sum: 1 } } },
        { $sort: { count: -1 } },
        { $limit: limit }
      ]).toArray();

      return tags.map(tag => tag._id);
    } catch (error) {
      return [];
    }
  }

  async getCollectionsByTags(tags, operator = 'AND', limit = 100) {
    try {
      const tagObjectIds = await this.collectionTags.find({
        tag: { $in: tags }
      }).toArray();

      const collectionIds = new Set();
      
      if (operator === 'AND') {
        // All tags must be present
        const tagMap = new Map();
        tagObjectIds.forEach(tag => {
          const collectionId = tag.collection_id.toString();
          if (!tagMap.has(collectionId)) {
            tagMap.set(collectionId, new Set());
          }
          tagMap.get(collectionId).add(tag.tag);
        });

        // Find collections that have all tags
        for (const [collectionId, collectionTags] of tagMap) {
          if (tags.every(tag => collectionTags.has(tag))) {
            collectionIds.add(collectionId);
          }
        }
      } else {
        // OR: any tag can be present
        tagObjectIds.forEach(tag => {
          collectionIds.add(tag.collection_id.toString());
        });
      }

      const objectIds = Array.from(collectionIds).map(id => new ObjectId(id));
      
      const collections = await this.collections.find({
        _id: { $in: objectIds }
      }).limit(limit).toArray();

      return collections;
    } catch (error) {
      return [];
    }
  }

  async getPopularCollections(limit = 10) {
    try {
      const stats = await this.collectionStats.find({})
        .sort({ view_count: -1 })
        .limit(limit)
        .toArray();

      const collectionIds = stats.map(stat => stat.collection_id);
      const collections = await this.collections.find({
        _id: { $in: collectionIds }
      }).toArray();

      // Combine with stats
      const collectionMap = new Map(collections.map(c => [c._id.toString(), c]));
      return stats.map(stat => ({
        ...collectionMap.get(stat.collection_id.toString()),
        statistics: stat
      }));
    } catch (error) {
      return [];
    }
  }

  async getPopularTags(limit = 20) {
    try {
      const tags = await this.collectionTags.aggregate([
        { $group: { _id: '$tag', count: { $sum: 1 } } },
        { $sort: { count: -1 } },
        { $limit: limit }
      ]).toArray();

      return tags.map(tag => ({
        tag: tag._id,
        count: tag.count
      }));
    } catch (error) {
      return [];
    }
  }

  async getAnalytics() {
    try {
      const [
        totalCollections,
        totalImages,
        totalTags,
        popularCollections,
        popularTags,
        totalViewTime
      ] = await Promise.all([
        this.collections.countDocuments(),
        this.images.countDocuments(),
        this.collectionTags.distinct('tag'),
        this.getPopularCollections(5),
        this.getPopularTags(10),
        this.collectionStats.aggregate([
          { $group: { _id: null, total: { $sum: '$total_view_time' } } }
        ]).toArray()
      ]);

      return {
        summary: {
          total_collections: totalCollections,
          total_images: totalImages,
          total_tags: totalTags.length,
          total_view_time: totalViewTime[0]?.total || 0
        },
        popular_collections: popularCollections,
        popular_tags: popularTags
      };
    } catch (error) {
      throw error;
    }
  }

  // Cache folder management methods
  async addCacheFolder(name, path, priority = 0, maxSize = null) {
    try {
      const cacheFolderData = {
        name,
        path,
        priority,
        max_size: maxSize,
        current_size: 0,
        file_count: 0,
        is_active: true,
        created_at: new Date(),
        updated_at: new Date()
      };

      const result = await this.cacheFolders.insertOne(cacheFolderData);
      return result.insertedId;
    } catch (error) {
      if (error.code === 11000) {
        throw new Error('Cache folder with this path already exists');
      }
      throw error;
    }
  }

  async getCacheFolders() {
    try {
      if (!this.cacheFolders) {
        return [];
      }
      const folders = await this.cacheFolders.find({ is_active: true })
        .sort({ priority: -1, created_at: 1 })
        .toArray();
      return folders || [];
    } catch (error) {
      console.error('Error getting cache folders:', error);
      return [];
    }
  }

  async updateCacheFolder(id, updates) {
    try {
      // Validate ObjectId format
      if (typeof id === 'string' && !ObjectId.isValid(id)) {
        throw new Error('Invalid cache folder ID format');
      }
      
      const objectId = typeof id === 'string' ? new ObjectId(id) : id;
      updates.updated_at = new Date();
      
      const result = await this.cacheFolders.updateOne(
        { _id: objectId },
        { $set: updates }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      console.error('Error updating cache folder:', error);
      throw error;
    }
  }

  async deleteCacheFolder(id) {
    try {
      // Validate ObjectId format
      if (typeof id === 'string' && !ObjectId.isValid(id)) {
        throw new Error('Invalid cache folder ID format');
      }
      
      const objectId = typeof id === 'string' ? new ObjectId(id) : id;
      
      // Remove cache folder bindings
      await this.collectionCacheBindings.deleteMany({ cache_folder_id: objectId });
      
      // Delete cache folder
      const result = await this.cacheFolders.deleteOne({ _id: objectId });
      return result.deletedCount > 0;
    } catch (error) {
      console.error('Error deleting cache folder:', error);
      throw error;
    }
  }

  async getCacheFolderStats() {
    try {
      if (!this.cacheFolders) {
        return {
          summary: { total_folders: 0, total_size: 0, total_files: 0, avg_priority: 0 },
          folders: []
        };
      }

      const stats = await this.cacheFolders.aggregate([
        { $match: { is_active: true } },
        {
          $group: {
            _id: null,
            total_folders: { $sum: 1 },
            total_size: { $sum: '$current_size' },
            total_files: { $sum: '$file_count' },
            avg_priority: { $avg: '$priority' }
          }
        }
      ]).toArray();

      const folderDetails = await this.cacheFolders.find({ is_active: true })
        .sort({ priority: -1 })
        .toArray();

      return {
        summary: stats[0] || { total_folders: 0, total_size: 0, total_files: 0, avg_priority: 0 },
        folders: folderDetails || []
      };
    } catch (error) {
      console.error('Error getting cache folder stats:', error);
      return {
        summary: { total_folders: 0, total_size: 0, total_files: 0, avg_priority: 0 },
        folders: []
      };
    }
  }

  // Collection cache binding methods
  async bindCollectionToCacheFolder(collectionId, cacheFolderId) {
    try {
      const collectionObjectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      const cacheFolderObjectId = typeof cacheFolderId === 'string' ? new ObjectId(cacheFolderId) : cacheFolderId;
      
      const bindingData = {
        collection_id: collectionObjectId,
        cache_folder_id: cacheFolderObjectId,
        created_at: new Date(),
        updated_at: new Date()
      };

      await this.collectionCacheBindings.replaceOne(
        { collection_id: collectionObjectId },
        bindingData,
        { upsert: true }
      );
      
      return true;
    } catch (error) {
      throw error;
    }
  }

  async getCollectionCacheFolder(collectionId) {
    try {
      const collectionObjectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      const binding = await this.collectionCacheBindings.findOne({ 
        collection_id: collectionObjectId 
      });
      
      if (!binding) return null;
      
      const cacheFolder = await this.cacheFolders.findOne({ 
        _id: binding.cache_folder_id 
      });
      
      return cacheFolder;
    } catch (error) {
      return null;
    }
  }

  async getCacheFolderForCollection(collectionId) {
    try {
      const collectionObjectId = typeof collectionId === 'string' ? new ObjectId(collectionId) : collectionId;
      
      // Check if collection already has a cache folder assigned
      const existingBinding = await this.collectionCacheBindings.findOne({ 
        collection_id: collectionObjectId 
      });
      
      if (existingBinding) {
        const cacheFolder = await this.cacheFolders.findOne({ 
          _id: existingBinding.cache_folder_id 
        });
        if (cacheFolder && cacheFolder.is_active) {
          return cacheFolder;
        }
      }
      
      // Find the best cache folder using distribution logic
      const cacheFolders = await this.cacheFolders.find({ 
        is_active: true 
      }).sort({ priority: -1, current_size: 1 }).toArray();
      
      if (cacheFolders.length === 0) {
        throw new Error('No active cache folders available');
      }
      
      // Use round-robin with priority weighting
      let selectedFolder = cacheFolders[0];
      
      // If we have multiple folders, use size-based distribution
      if (cacheFolders.length > 1) {
        // Find folder with least usage (considering max_size if set)
        let bestRatio = 1;
        for (const folder of cacheFolders) {
          const ratio = folder.max_size ? 
            folder.current_size / folder.max_size : 
            folder.current_size / (1024 * 1024 * 1024); // Default 1GB reference
          
          if (ratio < bestRatio) {
            bestRatio = ratio;
            selectedFolder = folder;
          }
        }
      }
      
      // Bind collection to selected cache folder
      await this.bindCollectionToCacheFolder(collectionObjectId, selectedFolder._id);
      
      return selectedFolder;
    } catch (error) {
      throw error;
    }
  }

  async updateCacheFolderUsage(cacheFolderId, sizeDelta, fileCountDelta) {
    try {
      const objectId = typeof cacheFolderId === 'string' ? new ObjectId(cacheFolderId) : cacheFolderId;
      
      const result = await this.cacheFolders.updateOne(
        { _id: objectId },
        { 
          $inc: { 
            current_size: sizeDelta,
            file_count: fileCountDelta
          },
          $set: { 
            updated_at: new Date()
          }
        }
      );
      
      return result.modifiedCount > 0;
    } catch (error) {
      throw error;
    }
  }

  // Utility methods
  async close() {
    if (this.client) {
      await this.client.close();
      console.log('[MONGODB] Connection closed');
    }
  }

  // Convert ObjectId to string for API responses
  toApiResponse(data) {
    if (Array.isArray(data)) {
      return data.map(item => this.toApiResponse(item));
    }
    
    if (data && typeof data === 'object' && data._id) {
      return {
        ...data,
        id: data._id.toString()
      };
    }
    
    return data;
  }

  // Convert string ID to ObjectId
  toObjectId(id) {
    if (typeof id === 'string') {
      return new ObjectId(id);
    }
    return id;
  }
}

module.exports = MongoDBDatabase;
