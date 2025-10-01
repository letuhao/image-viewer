const fs = require('fs');
const path = require('path');
const longPathHandler = require('../utils/longPathHandler');
const Logger = require('../utils/logger');

class TagService {
  constructor() {
    this.tagsData = null;
    this.logger = new Logger('TagService');
    this.loadTagsData();
  }

  loadTagsData() {
    try {
      const tagsPath = longPathHandler.joinSafe(__dirname, '../data/tags.json');
      const data = fs.readFileSync(tagsPath, 'utf8');
      this.tagsData = JSON.parse(data);
    } catch (error) {
      this.logger.error('Error loading tags data', { error: error.message, stack: error.stack });
      this.tagsData = { categories: {}, auto_tag_patterns: {} };
    }
  }

  // Get all available languages
  getAvailableLanguages() {
    return ['en', 'zh', 'ko', 'ja'];
  }

  // Get tag in specific language
  getTagInLanguage(tagKey, language = 'en') {
    if (!this.tagsData || !this.tagsData.categories) {
      return tagKey;
    }

    for (const category of Object.values(this.tagsData.categories)) {
      if (category.tags && category.tags[tagKey]) {
        return category.tags[tagKey][language] || category.tags[tagKey].en || tagKey;
      }
    }
    return tagKey;
  }

  // Get all tags for a category in specific language
  getCategoryTags(categoryKey, language = 'en') {
    if (!this.tagsData || !this.tagsData.categories[categoryKey]) {
      return [];
    }

    const category = this.tagsData.categories[categoryKey];
    const tags = [];

    for (const [tagKey, translations] of Object.entries(category.tags)) {
      tags.push({
        key: tagKey,
        name: translations[language] || translations.en || tagKey,
        translations: translations
      });
    }

    return tags;
  }

  // Get all categories in specific language
  getCategories(language = 'en') {
    if (!this.tagsData || !this.tagsData.categories) {
      return [];
    }

    const categories = [];
    for (const [categoryKey, category] of Object.entries(this.tagsData.categories)) {
      categories.push({
        key: categoryKey,
        name: category.name[language] || category.name.en || categoryKey,
        nameTranslations: category.name
      });
    }

    return categories;
  }

  // Auto-detect tags from collection name
  autoTagFromName(collectionName) {
    if (!this.tagsData || !this.tagsData.auto_tag_patterns) {
      return [];
    }

    const detectedTags = [];
    const name = collectionName.toLowerCase();

    // Helper function to check patterns for a category
    const checkPatterns = (patterns, tagKey) => {
      if (patterns && Array.isArray(patterns)) {
        if (patterns.some(pattern => name.includes(pattern.toLowerCase()))) {
          detectedTags.push(tagKey);
          return true;
        }
      }
      return false;
    };

    // Language detection - check all languages
    const languagePatterns = this.tagsData.auto_tag_patterns.language_detection || {};
    for (const [language, patterns] of Object.entries(languagePatterns)) {
      if (checkPatterns(patterns, language)) {
        break; // Only detect one primary language
      }
    }

    // Genre detection - comprehensive patterns across all languages
    const genrePatterns = this.tagsData.auto_tag_patterns.genre_detection || {};
    for (const [genre, patterns] of Object.entries(genrePatterns)) {
      checkPatterns(patterns, genre);
    }

    // Content rating detection - comprehensive patterns
    const ratingPatterns = this.tagsData.auto_tag_patterns.content_rating || {};
    for (const [rating, patterns] of Object.entries(ratingPatterns)) {
      checkPatterns(patterns, rating);
    }

    // Format detection - comprehensive patterns
    const formatPatterns = this.tagsData.auto_tag_patterns.format_detection || {};
    for (const [format, patterns] of Object.entries(formatPatterns)) {
      checkPatterns(patterns, format);
    }

    // Series detection patterns
    const seriesPatterns = this.tagsData.auto_tag_patterns.series_detection || {};
    for (const [seriesType, patterns] of Object.entries(seriesPatterns)) {
      checkPatterns(patterns, seriesType);
    }

    // Content type patterns
    const contentTypePatterns = this.tagsData.auto_tag_patterns.content_type || {};
    for (const [contentType, patterns] of Object.entries(contentTypePatterns)) {
      if (!detectedTags.includes(contentType)) {
        checkPatterns(patterns, contentType);
      }
    }

    return [...new Set(detectedTags)]; // Remove duplicates
  }

  // Search tags across all languages
  searchTags(query, language = 'en') {
    if (!this.tagsData || !this.tagsData.categories) {
      return [];
    }

    const results = [];
    const searchQuery = query.toLowerCase();

    for (const [categoryKey, category] of Object.entries(this.tagsData.categories)) {
      if (category.tags) {
        for (const [tagKey, translations] of Object.entries(category.tags)) {
          // Search in all languages
          const searchableText = Object.values(translations).join(' ').toLowerCase();
          if (searchableText.includes(searchQuery)) {
            results.push({
              key: tagKey,
              name: translations[language] || translations.en || tagKey,
              category: category.name[language] || category.name.en || categoryKey,
              translations: translations,
              relevance: this.calculateRelevance(searchQuery, translations)
            });
          }
        }
      }
    }

    // Sort by relevance
    return results.sort((a, b) => b.relevance - a.relevance);
  }

  // Calculate relevance score for search
  calculateRelevance(query, translations) {
    let score = 0;
    const queryLower = query.toLowerCase();

    for (const [lang, text] of Object.entries(translations)) {
      const textLower = text.toLowerCase();
      
      if (textLower === queryLower) {
        score += 100; // Exact match
      } else if (textLower.startsWith(queryLower)) {
        score += 50; // Starts with query
      } else if (textLower.includes(queryLower)) {
        score += 25; // Contains query
      }
    }

    return score;
  }

  // Get tag suggestions based on existing tags
  getTagSuggestions(existingTags = [], language = 'en') {
    if (!this.tagsData || !this.tagsData.categories) {
      return [];
    }

    const suggestions = [];
    const existingTagKeys = new Set(existingTags);

    // Find related tags in the same categories
    for (const [categoryKey, category] of Object.entries(this.tagsData.categories)) {
      if (category.tags) {
        let categoryTagCount = 0;
        for (const [tagKey, translations] of Object.entries(category.tags)) {
          if (existingTagKeys.has(tagKey)) {
            categoryTagCount++;
          }
        }

        // If category has some tags, suggest others from same category
        if (categoryTagCount > 0) {
          for (const [tagKey, translations] of Object.entries(category.tags)) {
            if (!existingTagKeys.has(tagKey)) {
              suggestions.push({
                key: tagKey,
                name: translations[language] || translations.en || tagKey,
                category: category.name[language] || category.name.en || categoryKey,
                reason: `Related to ${category.name[language] || category.name.en || categoryKey}`
              });
            }
          }
        }
      }
    }

    return suggestions.slice(0, 10); // Limit to 10 suggestions
  }

  // Translate tag to all supported languages
  translateTag(tagKey, fromLanguage = 'en') {
    if (!this.tagsData || !this.tagsData.categories) {
      return { [fromLanguage]: tagKey };
    }

    for (const category of Object.values(this.tagsData.categories)) {
      if (category.tags && category.tags[tagKey]) {
        return category.tags[tagKey];
      }
    }

    return { [fromLanguage]: tagKey };
  }

  // Get popular tags (this would typically come from database usage stats)
  getPopularTags(language = 'en', limit = 20) {
    // This would ideally be populated from actual usage data
    const popularTagKeys = [
      'manga', 'anime', 'romance', 'comedy', 'drama', 'action', 'fantasy',
      'yuri', 'yaoi', 'shoujo', 'shounen', 'ecchi', 'adult', 'vanilla',
      'full_color', 'black_and_white', 'english', 'japanese', 'chinese', 'korean'
    ];

    return popularTagKeys.slice(0, limit).map(tagKey => ({
      key: tagKey,
      name: this.getTagInLanguage(tagKey, language),
      translations: this.translateTag(tagKey, language)
    }));
  }
}

module.exports = new TagService();
