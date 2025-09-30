const express = require('express');
const router = express.Router();
const fs = require('fs');
const path = require('path');
const db = require('../database');

// Logger function
const log = (message) => {
  const timestamp = new Date().toISOString();
  const logMessage = `${timestamp}: ${message}\n`;
  fs.appendFileSync(path.join(__dirname, '../../logs/random.log'), logMessage);
  console.log(message); // Also log to console
};

log('[RANDOM] Random router loaded');

// Get random collection
router.get('/', async (req, res) => {
  log('[RANDOM] ===== API CALLED =====');
  try {
    log('[RANDOM] Starting random collection selection...');
    
    // Get total count of collections
    const totalCollections = await db.getCollectionCount();
    log(`[RANDOM] Total collections: ${totalCollections}`);
    
    if (totalCollections === 0) {
      log('[RANDOM] No collections found in database');
      return res.status(404).json({ error: 'No collections found' });
    }
    
    // Pick random index
    const randomIndex = Math.floor(Math.random() * totalCollections);
    log(`[RANDOM] Random index: ${randomIndex} out of ${totalCollections}`);
    
    // Get collection by index (skip randomIndex, limit 1)
    log(`[RANDOM] Fetching collection with skip: ${randomIndex}, limit: 1`);
    const collections = await db.getCollections({ skip: randomIndex, limit: 1 });
    log(`[RANDOM] Collections found: ${collections.length}`);
    log(`[RANDOM] Collections data: ${JSON.stringify(collections, null, 2)}`);
    
    if (collections.length === 0) {
      log('[RANDOM] No collection returned from database');
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    const randomCollection = collections[0];
    log(`[RANDOM] Selected collection: ${randomCollection.name}, ID: ${randomCollection.id}`);
    
    // Get statistics and tags for the random collection
    log(`[RANDOM] Fetching stats and tags for collection: ${randomCollection.id}`);
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
    
    log(`[RANDOM] Successfully returning collection: ${collectionWithStats.name}`);
    res.json(collectionWithStats);
  } catch (error) {
    log('[RANDOM] ===== ERROR OCCURRED =====');
    log(`[RANDOM] Error getting random collection: ${error.message}`);
    log(`[RANDOM] Error stack: ${error.stack}`);
    res.status(500).json({ error: 'Failed to get random collection' });
  }
});

module.exports = router;
