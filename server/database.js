// MongoDB Database Adapter - Compatibility layer for existing routes
const MongoDBDatabase = require('./mongodb');

let dbInstance;

// Initialize MongoDB connection
const initDatabase = async () => {
  if (!dbInstance) {
    dbInstance = new MongoDBDatabase();
    await dbInstance.connect();
  }
  return dbInstance;
};

// Compatibility class that mimics the old SQLite database interface
class Database {
  constructor() {
    // Initialize MongoDB connection
    initDatabase().then(db => {
      this.db = db;
    }).catch(error => {
      console.error('Database initialization failed:', error);
    });
  }

  async _ensureConnection() {
    if (!this.db) {
      this.db = await initDatabase();
    }
    return this.db;
  }

  // Collection methods
  async addCollection(name, path, type, metadata = {}) {
    const db = await this._ensureConnection();
    const id = await db.addCollection(name, path, type, metadata);
    return id.toString();
  }

  async getCollection(id) {
    const db = await this._ensureConnection();
    const collection = await db.getCollection(id);
    return collection ? db.toApiResponse(collection) : null;
  }

  async getCollectionByPath(path) {
    const db = await this._ensureConnection();
    const collection = await db.getCollectionByPath(path);
    return collection ? db.toApiResponse(collection) : null;
  }

  async getAllCollections() {
    const db = await this._ensureConnection();
    const collections = await db.collections.find({}).toArray();
    
    // Convert _id to id for frontend compatibility
    collections.forEach(collection => {
      collection.id = collection._id.toString();
    });
    
    return collections;
  }

  // Get collections with pagination support
  async getCollections(options = {}) {
    const { skip = 0, limit = 1000 } = options;
    const db = await this._ensureConnection();
    const collections = await db.collections.find({}).skip(skip).limit(limit).toArray();
    
    // Convert _id to id for frontend compatibility
    collections.forEach(collection => {
      collection.id = collection._id.toString();
    });
    
    return collections;
  }

  // Get total count of collections
  async getCollectionCount() {
    const db = await this._ensureConnection();
    return await db.collections.countDocuments({});
  }

  async getImageCount(collectionId) {
    const db = await this._ensureConnection();
    const count = await db.images.countDocuments({ 
      collection_id: db.toObjectId(collectionId) 
    });
    return count;
  }

  async addImages(images) {
    const db = await this._ensureConnection();
    const insertPromises = images.map(image => this.addImage(image.collection_id, image));
    return await Promise.all(insertPromises);
  }

  async deleteImages(collectionId) {
    return await this.deleteCollectionImages(collectionId);
  }

  async clearExpiredCache() {
    const db = await this._ensureConnection();
    const result = await db.cache.deleteMany({
      expires_at: { $lte: new Date() }
    });
    return result.deletedCount;
  }

  async updateCollection(id, updates) {
    const db = await this._ensureConnection();
    return await db.updateCollection(id, updates);
  }

  async deleteCollection(id) {
    const db = await this._ensureConnection();
    return await db.deleteCollection(id);
  }

  // Image methods
  async addImage(collectionId, imageData) {
    const db = await this._ensureConnection();
    const id = await db.addImage(collectionId, imageData);
    return id.toString();
  }

  async getImages(collectionId, options = {}) {
    const db = await this._ensureConnection();
    const images = await db.getImages(collectionId, options);
    return db.toApiResponse(images);
  }

  async getImage(collectionId, imageId) {
    const db = await this._ensureConnection();
    const image = await db.getImage(collectionId, imageId);
    return image ? db.toApiResponse(image) : null;
  }

  async deleteImage(collectionId, imageId) {
    const db = await this._ensureConnection();
    return await db.deleteImage(collectionId, imageId);
  }

  async deleteCollectionImages(collectionId) {
    const db = await this._ensureConnection();
    return await db.deleteCollectionImages(collectionId);
  }

  // Cache methods
  async setCache(key, value, ttl = null) {
    const db = await this._ensureConnection();
    return await db.setCache(key, value, ttl);
  }

  async getCache(key) {
    const db = await this._ensureConnection();
    return await db.getCache(key);
  }

  async deleteCache(key) {
    const db = await this._ensureConnection();
    return await db.deleteCache(key);
  }

  // Collection statistics methods
  async initializeCollectionStats(collectionId) {
    const db = await this._ensureConnection();
    return await db.initializeCollectionStats(collectionId);
  }

  async getCollectionStats(collectionId) {
    const db = await this._ensureConnection();
    const stats = await db.getCollectionStats(collectionId);
    return stats ? db.toApiResponse(stats) : null;
  }

  async incrementViewCount(collectionId) {
    const db = await this._ensureConnection();
    return await db.incrementViewCount(collectionId);
  }

  async incrementSearchCount(collectionId) {
    const db = await this._ensureConnection();
    return await db.incrementSearchCount(collectionId);
  }

  async addViewTime(collectionId, timeInSeconds) {
    const db = await this._ensureConnection();
    return await db.addViewTime(collectionId, timeInSeconds);
  }

  async startViewSession(collectionId, sessionId) {
    const db = await this._ensureConnection();
    const id = await db.startViewSession(collectionId, sessionId);
    return id.toString();
  }

  async endViewSession(sessionId, totalTime) {
    const db = await this._ensureConnection();
    return await db.endViewSession(sessionId, totalTime);
  }

  // Collection tags methods
  async getCollectionTags(collectionId) {
    const db = await this._ensureConnection();
    return await db.getCollectionTags(collectionId);
  }

  async addTagToCollection(collectionId, tag, addedBy = 'anonymous') {
    const db = await this._ensureConnection();
    return await db.addTagToCollection(collectionId, tag, addedBy);
  }

  async removeTagFromCollection(collectionId, tag, addedBy = null) {
    const db = await this._ensureConnection();
    return await db.removeTagFromCollection(collectionId, tag, addedBy);
  }

  async searchTags(query, limit = 10) {
    const db = await this._ensureConnection();
    return await db.searchTags(query, limit);
  }

  async getTagSuggestions(query, limit = 10) {
    const db = await this._ensureConnection();
    return await db.getTagSuggestions(query, limit);
  }

  async getCollectionsByTags(tags, operator = 'AND', limit = 100) {
    const db = await this._ensureConnection();
    const collections = await db.getCollectionsByTags(tags, operator, limit);
    return db.toApiResponse(collections);
  }

  async getPopularCollections(limit = 10) {
    const db = await this._ensureConnection();
    const collections = await db.getPopularCollections(limit);
    return db.toApiResponse(collections);
  }

  async getPopularTags(limit = 20) {
    const db = await this._ensureConnection();
    return await db.getPopularTags(limit);
  }

  async getAnalytics() {
    const db = await this._ensureConnection();
    return await db.getAnalytics();
  }

  // Cache folder management methods
  async addCacheFolder(name, path, priority = 0, maxSize = null) {
    const db = await this._ensureConnection();
    const id = await db.addCacheFolder(name, path, priority, maxSize);
    return id.toString();
  }

  async getCacheFolders() {
    const db = await this._ensureConnection();
    const folders = await db.getCacheFolders();
    return db.toApiResponse(folders);
  }

  async updateCacheFolder(id, updates) {
    const db = await this._ensureConnection();
    return await db.updateCacheFolder(id, updates);
  }

  async deleteCacheFolder(id) {
    const db = await this._ensureConnection();
    return await db.deleteCacheFolder(id);
  }

  async getCacheFolderStats() {
    const db = await this._ensureConnection();
    return await db.getCacheFolderStats();
  }

  async bindCollectionToCacheFolder(collectionId, cacheFolderId) {
    const db = await this._ensureConnection();
    return await db.bindCollectionToCacheFolder(collectionId, cacheFolderId);
  }

  async getCollectionCacheFolder(collectionId) {
    const db = await this._ensureConnection();
    const folder = await db.getCollectionCacheFolder(collectionId);
    return folder ? db.toApiResponse(folder) : null;
  }

  async getCacheFolderForCollection(collectionId) {
    const db = await this._ensureConnection();
    return await db.getCacheFolderForCollection(collectionId);
  }

  async updateCacheFolderUsage(cacheFolderId, sizeDelta, fileCountDelta) {
    const db = await this._ensureConnection();
    return await db.updateCacheFolderUsage(cacheFolderId, sizeDelta, fileCountDelta);
  }

  async getCollectionCacheStatus(collectionId) {
    const db = await this._ensureConnection();
    return await db.getCollectionCacheStatus(collectionId);
  }

  async updateImage(imageId, updateData) {
    const db = await this._ensureConnection();
    return await db.updateImage(imageId, updateData);
  }

  async getCacheFolderByPath(cachePath) {
    const db = await this._ensureConnection();
    return await db.getCacheFolderByPath(cachePath);
  }
}

// Export a singleton instance
const database = new Database();

module.exports = database;