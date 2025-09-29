const express = require('express');
const router = express.Router();
const crypto = require('crypto');
const db = require('../database');
const tagService = require('../services/tagService');

// Get collection statistics
router.get('/collection/:id', async (req, res) => {
  try {
    const collectionId = req.params.id;
    
    // Check if collection exists
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Get statistics
    const stats = await db.getCollectionStats(collectionId);
    const tags = await db.getCollectionTags(collectionId);
    
    res.json({
      collection: collection,
      statistics: stats || {
        view_count: 0,
        total_view_time: 0,
        search_count: 0,
        last_viewed: null,
        last_searched: null
      },
      tags: tags
    });
  } catch (error) {
    console.error('Error fetching collection statistics:', error);
    res.status(500).json({ error: 'Failed to fetch statistics' });
  }
});

// Track collection view
router.post('/collection/:id/view', async (req, res) => {
  try {
    const collectionId = req.params.id;
    
    // Check if collection exists
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Increment view count
    await db.incrementViewCount(collectionId);
    
    // Start view session if session_id provided
    const { session_id } = req.body;
    if (session_id) {
      await db.startViewSession(collectionId, session_id);
    }
    
    res.json({ message: 'View tracked successfully' });
  } catch (error) {
    console.error('Error tracking view:', error);
    res.status(500).json({ error: 'Failed to track view' });
  }
});

// End collection view session
router.post('/collection/:id/view/end', async (req, res) => {
  try {
    const collectionId = req.params.id;
    const { session_id, view_time_seconds } = req.body;
    
    if (!session_id) {
      return res.status(400).json({ error: 'Session ID is required' });
    }
    
    // End view session
    await db.endViewSession(collectionId, session_id);
    
    // Add view time if provided
    if (view_time_seconds && view_time_seconds > 0) {
      await db.addViewTime(collectionId, view_time_seconds);
    }
    
    res.json({ message: 'View session ended successfully' });
  } catch (error) {
    console.error('Error ending view session:', error);
    res.status(500).json({ error: 'Failed to end view session' });
  }
});

// Track collection search
router.post('/collection/:id/search', async (req, res) => {
  try {
    const collectionId = req.params.id;
    const { query } = req.body;
    
    // Check if collection exists
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Increment search count
    await db.incrementSearchCount(collectionId);
    
    res.json({ 
      message: 'Search tracked successfully',
      query: query || 'unknown'
    });
  } catch (error) {
    console.error('Error tracking search:', error);
    res.status(500).json({ error: 'Failed to track search' });
  }
});

// Add tag to collection
router.post('/collection/:id/tags', async (req, res) => {
  try {
    const collectionId = req.params.id;
    const { tag, added_by = 'anonymous' } = req.body;
    
    if (!tag || !tag.trim()) {
      return res.status(400).json({ error: 'Tag is required' });
    }
    
    // Check if collection exists
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Add tag
    await db.addTagToCollection(collectionId, tag.trim(), added_by);
    
    // Get updated tags
    const tags = await db.getCollectionTags(collectionId);
    
    res.json({ 
      message: 'Tag added successfully',
      tags: tags
    });
  } catch (error) {
    console.error('Error adding tag:', error);
    res.status(500).json({ error: 'Failed to add tag' });
  }
});

// Remove tag from collection
router.delete('/collection/:id/tags/:tag', async (req, res) => {
  try {
    const collectionId = req.params.id;
    const tag = decodeURIComponent(req.params.tag);
    const { added_by } = req.query;
    
    // Check if collection exists
    const collection = await db.getCollection(collectionId);
    if (!collection) {
      return res.status(404).json({ error: 'Collection not found' });
    }
    
    // Remove tag
    const removed = await db.removeTagFromCollection(collectionId, tag, added_by);
    
    if (removed === 0) {
      return res.status(404).json({ error: 'Tag not found' });
    }
    
    // Get updated tags
    const tags = await db.getCollectionTags(collectionId);
    
    res.json({ 
      message: 'Tag removed successfully',
      tags: tags
    });
  } catch (error) {
    console.error('Error removing tag:', error);
    res.status(500).json({ error: 'Failed to remove tag' });
  }
});

// Get popular collections
router.get('/popular', async (req, res) => {
  try {
    const { limit = 10 } = req.query;
    const popularCollections = await db.getPopularCollections(parseInt(limit));
    
    res.json({
      collections: popularCollections,
      limit: parseInt(limit)
    });
  } catch (error) {
    console.error('Error fetching popular collections:', error);
    res.status(500).json({ error: 'Failed to fetch popular collections' });
  }
});

// Get popular tags
router.get('/tags/popular', async (req, res) => {
  try {
    const { limit = 20 } = req.query;
    const popularTags = await db.getPopularTags(parseInt(limit));
    
    res.json({
      tags: popularTags,
      limit: parseInt(limit)
    });
  } catch (error) {
    console.error('Error fetching popular tags:', error);
    res.status(500).json({ error: 'Failed to fetch popular tags' });
  }
});

// Get analytics dashboard data
router.get('/analytics', async (req, res) => {
  try {
    const popularCollections = await db.getPopularCollections(10);
    const popularTags = await db.getPopularTags(20);
    
    // Get total statistics
    const totalCollections = await db.getCollections();
    const totalImages = totalCollections.reduce((sum, col) => sum + (col.image_count || 0), 0);
    
    res.json({
      summary: {
        total_collections: totalCollections.length,
        total_images: totalImages,
        most_viewed_collection: popularCollections[0] || null,
        most_tagged_collection: popularCollections.sort((a, b) => (b.tag_count || 0) - (a.tag_count || 0))[0] || null
      },
      popular_collections: popularCollections,
      popular_tags: popularTags
    });
  } catch (error) {
    console.error('Error fetching analytics:', error);
    res.status(500).json({ error: 'Failed to fetch analytics' });
  }
});

// Search tags (for autocomplete)
router.get('/tags/search', async (req, res) => {
  try {
    const { q = '', limit = 10 } = req.query;
    
    const tags = await db.searchTags(q, parseInt(limit));
    
    res.json({
      tags: tags,
      query: q,
      limit: parseInt(limit)
    });
  } catch (error) {
    console.error('Error searching tags:', error);
    res.status(500).json({ error: 'Failed to search tags' });
  }
});

// Get collections by tags
router.get('/collections/by-tags', async (req, res) => {
  try {
    const { tags, operator = 'AND', limit = 50, offset = 0 } = req.query;
    
    if (!tags) {
      return res.status(400).json({ error: 'Tags parameter is required' });
    }
    
    const tagArray = Array.isArray(tags) ? tags : tags.split(',').map(t => t.trim());
    const collections = await db.getCollectionsByTags(tagArray, operator, parseInt(limit), parseInt(offset));
    
    res.json({
      collections: collections,
      tags: tagArray,
      operator: operator,
      limit: parseInt(limit),
      offset: parseInt(offset)
    });
  } catch (error) {
    console.error('Error fetching collections by tags:', error);
    res.status(500).json({ error: 'Failed to fetch collections by tags' });
  }
});

// Get tag suggestions based on partial input
router.get('/tags/suggestions', async (req, res) => {
  try {
    const { q = '', limit = 5 } = req.query;
    
    const suggestions = await db.getTagSuggestions(q, parseInt(limit));
    
    res.json({
      suggestions: suggestions,
      query: q,
      limit: parseInt(limit)
    });
  } catch (error) {
    console.error('Error fetching tag suggestions:', error);
    res.status(500).json({ error: 'Failed to fetch tag suggestions' });
  }
});

// Get available languages for tags
router.get('/tags/languages', async (req, res) => {
  try {
    const languages = tagService.getAvailableLanguages();
    res.json({ languages });
  } catch (error) {
    console.error('Error fetching available languages:', error);
    res.status(500).json({ error: 'Failed to fetch available languages' });
  }
});

// Get tag categories
router.get('/tags/categories', async (req, res) => {
  try {
    const { language = 'en' } = req.query;
    const categories = tagService.getCategories(language);
    res.json({ categories, language });
  } catch (error) {
    console.error('Error fetching tag categories:', error);
    res.status(500).json({ error: 'Failed to fetch tag categories' });
  }
});

// Get tags for a specific category
router.get('/tags/category/:categoryKey', async (req, res) => {
  try {
    const { categoryKey } = req.params;
    const { language = 'en' } = req.query;
    const tags = tagService.getCategoryTags(categoryKey, language);
    res.json({ tags, categoryKey, language });
  } catch (error) {
    console.error('Error fetching category tags:', error);
    res.status(500).json({ error: 'Failed to fetch category tags' });
  }
});

// Search tags using the tag service
router.get('/tags/service/search', async (req, res) => {
  try {
    const { q = '', language = 'en', limit = 20 } = req.query;
    const results = tagService.searchTags(q, language);
    res.json({ 
      results: results.slice(0, parseInt(limit)), 
      query: q, 
      language, 
      total: results.length 
    });
  } catch (error) {
    console.error('Error searching tags with service:', error);
    res.status(500).json({ error: 'Failed to search tags' });
  }
});

// Get popular tags from service
router.get('/tags/service/popular', async (req, res) => {
  try {
    const { language = 'en', limit = 20 } = req.query;
    const popularTags = tagService.getPopularTags(language, parseInt(limit));
    res.json({ tags: popularTags, language });
  } catch (error) {
    console.error('Error fetching popular tags:', error);
    res.status(500).json({ error: 'Failed to fetch popular tags' });
  }
});

// Get tag suggestions based on existing tags
router.get('/tags/service/suggestions', async (req, res) => {
  try {
    const { tags = '', language = 'en', limit = 10 } = req.query;
    const existingTags = tags ? tags.split(',').map(t => t.trim()) : [];
    const suggestions = tagService.getTagSuggestions(existingTags, language);
    res.json({ 
      suggestions: suggestions.slice(0, parseInt(limit)), 
      existingTags, 
      language 
    });
  } catch (error) {
    console.error('Error fetching tag suggestions:', error);
    res.status(500).json({ error: 'Failed to fetch tag suggestions' });
  }
});

// Translate a tag to all supported languages
router.get('/tags/service/translate/:tagKey', async (req, res) => {
  try {
    const { tagKey } = req.params;
    const { fromLanguage = 'en' } = req.query;
    const translations = tagService.translateTag(tagKey, fromLanguage);
    res.json({ tagKey, translations, fromLanguage });
  } catch (error) {
    console.error('Error translating tag:', error);
    res.status(500).json({ error: 'Failed to translate tag' });
  }
});

module.exports = router;
