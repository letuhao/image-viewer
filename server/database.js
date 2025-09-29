const sqlite3 = require('sqlite3').verbose();
const path = require('path');

class Database {
  constructor() {
    this.db = new sqlite3.Database(path.join(__dirname, 'database.sqlite'));
    this.init();
  }

  init() {
    this.db.serialize(() => {
      // Collections table
      this.db.run(`
        CREATE TABLE IF NOT EXISTS collections (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          name TEXT NOT NULL,
          path TEXT NOT NULL,
          type TEXT NOT NULL CHECK (type IN ('folder', 'zip', '7z', 'rar', 'tar')),
          created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          settings TEXT DEFAULT '{}'
        )
      `);

      // Images table
      this.db.run(`
        CREATE TABLE IF NOT EXISTS images (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          collection_id INTEGER NOT NULL,
          filename TEXT NOT NULL,
          relative_path TEXT NOT NULL,
          file_size INTEGER,
          width INTEGER,
          height INTEGER,
          thumbnail_path TEXT,
          created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          FOREIGN KEY (collection_id) REFERENCES collections (id) ON DELETE CASCADE
        )
      `);

      // Cache table for performance
      this.db.run(`
        CREATE TABLE IF NOT EXISTS cache (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          key TEXT UNIQUE NOT NULL,
          value TEXT NOT NULL,
          expires_at DATETIME,
          created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
      `);

      // Collection statistics table
      this.db.run(`
        CREATE TABLE IF NOT EXISTS collection_stats (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          collection_id INTEGER NOT NULL,
          view_count INTEGER DEFAULT 0,
          total_view_time INTEGER DEFAULT 0,
          search_count INTEGER DEFAULT 0,
          last_viewed DATETIME,
          last_searched DATETIME,
          created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          FOREIGN KEY (collection_id) REFERENCES collections (id) ON DELETE CASCADE
        )
      `);

      // Collection tags table
      this.db.run(`
        CREATE TABLE IF NOT EXISTS collection_tags (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          collection_id INTEGER NOT NULL,
          tag TEXT NOT NULL,
          added_by TEXT DEFAULT 'anonymous',
          added_at DATETIME DEFAULT CURRENT_TIMESTAMP,
          FOREIGN KEY (collection_id) REFERENCES collections (id) ON DELETE CASCADE,
          UNIQUE(collection_id, tag, added_by)
        )
      `);

      // Collection sessions table (for tracking view time)
      this.db.run(`
        CREATE TABLE IF NOT EXISTS collection_sessions (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          collection_id INTEGER NOT NULL,
          session_id TEXT NOT NULL,
          start_time DATETIME DEFAULT CURRENT_TIMESTAMP,
          end_time DATETIME,
          total_time INTEGER DEFAULT 0,
          FOREIGN KEY (collection_id) REFERENCES collections (id) ON DELETE CASCADE
        )
      `);

      // Indexes for performance
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_images_collection_id ON images (collection_id)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_images_filename ON images (filename)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_cache_key ON cache (key)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_collection_stats_collection_id ON collection_stats (collection_id)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_collection_tags_collection_id ON collection_tags (collection_id)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_collection_tags_tag ON collection_tags (tag)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_collection_sessions_collection_id ON collection_sessions (collection_id)`);
      this.db.run(`CREATE INDEX IF NOT EXISTS idx_cache_expires ON cache (expires_at)`);
    });
  }

  // Collection methods
  async addCollection(name, path, type, metadata = {}) {
      return new Promise(async (resolve, reject) => {
        try {
          const dbInstance = this; // Capture the database instance
          const stmt = this.db.prepare(`
            INSERT INTO collections (name, path, type, settings)
            VALUES (?, ?, ?, ?)
          `);
          stmt.run([name, path, type, JSON.stringify(metadata)], async function(err) {
            if (err) {
              reject(err);
              return;
            }

            const collectionId = this.lastID;
            
            // Auto-add tags from metadata if they exist
            if (metadata.auto_tags && Array.isArray(metadata.auto_tags) && metadata.auto_tags.length > 0) {
              try {
                await dbInstance.initializeCollectionStats(collectionId);
                
                // Add each auto-tag
                for (const tag of metadata.auto_tags) {
                  await dbInstance.addTagToCollection(collectionId, tag, 'system');
                }
              } catch (tagError) {
                console.error('Error adding auto-tags:', tagError);
                // Don't fail the collection creation if tag addition fails
              }
            } else {
              // Initialize stats even without tags
              await dbInstance.initializeCollectionStats(collectionId);
            }
            
            resolve(collectionId);
          });
          stmt.finalize();
        } catch (error) {
          reject(error);
        }
      });
  }

  async getCollections() {
    return new Promise((resolve, reject) => {
      this.db.all('SELECT * FROM collections ORDER BY created_at DESC', (err, rows) => {
        if (err) reject(err);
        else resolve(rows.map(row => ({
          ...row,
          settings: JSON.parse(row.settings || '{}')
        })));
      });
    });
  }

  async getCollection(id) {
    return new Promise((resolve, reject) => {
      this.db.get('SELECT * FROM collections WHERE id = ?', [id], (err, row) => {
        if (err) reject(err);
        else if (row) {
          resolve({
            ...row,
            settings: JSON.parse(row.settings || '{}')
          });
        } else {
          resolve(null);
        }
      });
    });
  }

  async updateCollection(id, updates) {
    return new Promise((resolve, reject) => {
      const fields = [];
      const values = [];
      
      Object.keys(updates).forEach(key => {
        if (key === 'settings') {
          fields.push(`${key} = ?`);
          values.push(JSON.stringify(updates[key]));
        } else {
          fields.push(`${key} = ?`);
          values.push(updates[key]);
        }
      });
      
      fields.push('updated_at = CURRENT_TIMESTAMP');
      values.push(id);
      
      const stmt = this.db.prepare(`
        UPDATE collections 
        SET ${fields.join(', ')} 
        WHERE id = ?
      `);
      
      stmt.run(values, function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
      stmt.finalize();
    });
  }

  async deleteCollection(id) {
    return new Promise((resolve, reject) => {
      this.db.run('DELETE FROM collections WHERE id = ?', [id], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  // Image methods
  async addImages(images) {
    return new Promise((resolve, reject) => {
      const stmt = this.db.prepare(`
        INSERT INTO images (collection_id, filename, relative_path, file_size, width, height, thumbnail_path)
        VALUES (?, ?, ?, ?, ?, ?, ?)
      `);
      
      this.db.serialize(() => {
        this.db.run('BEGIN TRANSACTION');
        
        images.forEach(image => {
          stmt.run([
            image.collection_id,
            image.filename,
            image.relative_path,
            image.file_size,
            image.width,
            image.height,
            image.thumbnail_path
          ]);
        });
        
        this.db.run('COMMIT', (err) => {
          if (err) reject(err);
          else resolve();
        });
      });
      
      stmt.finalize();
    });
  }

  async getImages(collectionId, options = {}) {
    return new Promise((resolve, reject) => {
      let query = 'SELECT * FROM images WHERE collection_id = ?';
      const params = [collectionId];
      
      query += ' ORDER BY filename';
      
      if (options.limit) {
        query += ' LIMIT ?';
        params.push(options.limit);
      }
      
      if (options.offset) {
        query += ' OFFSET ?';
        params.push(options.offset);
      }
      
      this.db.all(query, params, (err, rows) => {
        if (err) reject(err);
        else resolve(rows);
      });
    });
  }

  async getImageCount(collectionId) {
    return new Promise((resolve, reject) => {
      this.db.get(
        'SELECT COUNT(*) as count FROM images WHERE collection_id = ?',
        [collectionId],
        (err, row) => {
          if (err) reject(err);
          else resolve(row.count);
        }
      );
    });
  }

  async deleteImages(collectionId) {
    return new Promise((resolve, reject) => {
      this.db.run('DELETE FROM images WHERE collection_id = ?', [collectionId], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  // Cache methods
  async setCache(key, value, ttl = 3600000) { // 1 hour default TTL
    return new Promise((resolve, reject) => {
      const expiresAt = new Date(Date.now() + ttl).toISOString();
      const stmt = this.db.prepare(`
        INSERT OR REPLACE INTO cache (key, value, expires_at)
        VALUES (?, ?, ?)
      `);
      stmt.run([key, JSON.stringify(value), expiresAt], function(err) {
        if (err) reject(err);
        else resolve();
      });
      stmt.finalize();
    });
  }

  async getCache(key) {
    return new Promise((resolve, reject) => {
      this.db.get(
        'SELECT value FROM cache WHERE key = ? AND (expires_at IS NULL OR expires_at > datetime("now"))',
        [key],
        (err, row) => {
          if (err) reject(err);
          else if (row) {
            resolve(JSON.parse(row.value));
          } else {
            resolve(null);
          }
        }
      );
    });
  }

  async clearExpiredCache() {
    return new Promise((resolve, reject) => {
      this.db.run(
        'DELETE FROM cache WHERE expires_at IS NOT NULL AND expires_at <= datetime("now")',
        function(err) {
          if (err) reject(err);
          else resolve(this.changes);
        }
      );
    });
  }

  // Statistics methods
  async getCollectionStats(collectionId) {
    return new Promise((resolve, reject) => {
      this.db.get(
        'SELECT * FROM collection_stats WHERE collection_id = ?',
        [collectionId],
        (err, row) => {
          if (err) reject(err);
          else resolve(row);
        }
      );
    });
  }

  async initializeCollectionStats(collectionId) {
    return new Promise((resolve, reject) => {
      const stmt = this.db.prepare(`
        INSERT OR IGNORE INTO collection_stats (collection_id)
        VALUES (?)
      `);
      stmt.run([collectionId], function(err) {
        if (err) reject(err);
        else resolve(this.lastID);
      });
      stmt.finalize();
    });
  }

  async incrementViewCount(collectionId) {
    await this.initializeCollectionStats(collectionId);
    return new Promise((resolve, reject) => {
      this.db.run(`
        UPDATE collection_stats 
        SET view_count = view_count + 1, 
            last_viewed = datetime('now'),
            updated_at = datetime('now')
        WHERE collection_id = ?
      `, [collectionId], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  async incrementSearchCount(collectionId) {
    await this.initializeCollectionStats(collectionId);
    return new Promise((resolve, reject) => {
      this.db.run(`
        UPDATE collection_stats 
        SET search_count = search_count + 1, 
            last_searched = datetime('now'),
            updated_at = datetime('now')
        WHERE collection_id = ?
      `, [collectionId], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  async addViewTime(collectionId, timeInSeconds) {
    await this.initializeCollectionStats(collectionId);
    return new Promise((resolve, reject) => {
      this.db.run(`
        UPDATE collection_stats 
        SET total_view_time = total_view_time + ?,
            updated_at = datetime('now')
        WHERE collection_id = ?
      `, [timeInSeconds, collectionId], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  async startViewSession(collectionId, sessionId) {
    return new Promise((resolve, reject) => {
      const stmt = this.db.prepare(`
        INSERT INTO collection_sessions (collection_id, session_id)
        VALUES (?, ?)
      `);
      stmt.run([collectionId, sessionId], function(err) {
        if (err) reject(err);
        else resolve(this.lastID);
      });
      stmt.finalize();
    });
  }

  async endViewSession(collectionId, sessionId) {
    return new Promise((resolve, reject) => {
      this.db.run(`
        UPDATE collection_sessions 
        SET end_time = datetime('now'),
            total_time = (strftime('%s', datetime('now')) - strftime('%s', start_time))
        WHERE collection_id = ? AND session_id = ? AND end_time IS NULL
      `, [collectionId, sessionId], function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  // Tags methods
  async getCollectionTags(collectionId) {
    return new Promise((resolve, reject) => {
      this.db.all(
        `SELECT tag, COUNT(*) as count, GROUP_CONCAT(added_by) as added_by_list
         FROM collection_tags 
         WHERE collection_id = ?
         GROUP BY tag
         ORDER BY count DESC, tag ASC`,
        [collectionId],
        (err, rows) => {
          if (err) reject(err);
          else resolve(rows);
        }
      );
    });
  }

  async addTagToCollection(collectionId, tag, addedBy = 'anonymous') {
    return new Promise((resolve, reject) => {
      const stmt = this.db.prepare(`
        INSERT OR IGNORE INTO collection_tags (collection_id, tag, added_by)
        VALUES (?, ?, ?)
      `);
      stmt.run([collectionId, tag.toLowerCase().trim(), addedBy], function(err) {
        if (err) reject(err);
        else resolve(this.lastID);
      });
      stmt.finalize();
    });
  }

  async removeTagFromCollection(collectionId, tag, addedBy = null) {
    return new Promise((resolve, reject) => {
      let query = 'DELETE FROM collection_tags WHERE collection_id = ? AND tag = ?';
      let params = [collectionId, tag.toLowerCase().trim()];
      
      if (addedBy) {
        query += ' AND added_by = ?';
        params.push(addedBy);
      }
      
      this.db.run(query, params, function(err) {
        if (err) reject(err);
        else resolve(this.changes);
      });
    });
  }

  async getPopularCollections(limit = 10) {
    return new Promise((resolve, reject) => {
      this.db.all(`
        SELECT c.*, 
               COALESCE(cs.view_count, 0) as view_count,
               COALESCE(cs.total_view_time, 0) as total_view_time,
               COALESCE(cs.search_count, 0) as search_count,
               cs.last_viewed,
               (SELECT COUNT(*) FROM collection_tags ct WHERE ct.collection_id = c.id) as tag_count
        FROM collections c
        LEFT JOIN collection_stats cs ON c.id = cs.collection_id
        ORDER BY cs.view_count DESC, cs.total_view_time DESC, tag_count DESC
        LIMIT ?
      `, [limit], (err, rows) => {
        if (err) reject(err);
        else resolve(rows);
      });
    });
  }

  async getPopularTags(limit = 20) {
    return new Promise((resolve, reject) => {
      this.db.all(`
        SELECT tag, COUNT(*) as usage_count, COUNT(DISTINCT collection_id) as collection_count
        FROM collection_tags
        GROUP BY tag
        ORDER BY usage_count DESC, collection_count DESC
        LIMIT ?
      `, [limit], (err, rows) => {
        if (err) reject(err);
        else resolve(rows);
      });
    });
  }

  async searchTags(query, limit = 10) {
    return new Promise((resolve, reject) => {
      this.db.all(`
        SELECT tag, COUNT(*) as usage_count, COUNT(DISTINCT collection_id) as collection_count
        FROM collection_tags
        WHERE tag LIKE ?
        GROUP BY tag
        ORDER BY usage_count DESC, collection_count DESC
        LIMIT ?
      `, [`%${query}%`, limit], (err, rows) => {
        if (err) reject(err);
        else resolve(rows);
      });
    });
  }

  async getTagSuggestions(query, limit = 5) {
    return new Promise((resolve, reject) => {
      this.db.all(`
        SELECT DISTINCT tag, COUNT(*) as usage_count
        FROM collection_tags
        WHERE tag LIKE ?
        GROUP BY tag
        ORDER BY usage_count DESC, tag ASC
        LIMIT ?
      `, [`${query}%`, limit], (err, rows) => {
        if (err) reject(err);
        else resolve(rows.map(row => row.tag));
      });
    });
  }

  async getCollectionsByTags(tags, operator = 'AND', limit = 50, offset = 0) {
    return new Promise((resolve, reject) => {
      // Build the query based on operator
      let tagConditions = tags.map(() => 'ct.tag = ?').join(operator === 'AND' ? ' AND ' : ' OR ');
      
      let query = `
        SELECT DISTINCT c.*, 
               COALESCE(cs.view_count, 0) as view_count,
               COALESCE(cs.total_view_time, 0) as total_view_time,
               COALESCE(cs.search_count, 0) as search_count,
               cs.last_viewed,
               (SELECT COUNT(*) FROM collection_tags ct2 WHERE ct2.collection_id = c.id) as tag_count
        FROM collections c
        INNER JOIN collection_tags ct ON c.id = ct.collection_id
        LEFT JOIN collection_stats cs ON c.id = cs.collection_id
        WHERE ${tagConditions}
        ORDER BY cs.view_count DESC, tag_count DESC
        LIMIT ? OFFSET ?
      `;
      
      this.db.all(query, [...tags, limit, offset], (err, rows) => {
        if (err) reject(err);
        else resolve(rows);
      });
    });
  }

  close() {
    this.db.close();
  }
}

module.exports = new Database();
