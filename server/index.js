const express = require('express');
const cors = require('cors');
const path = require('path');
const compression = require('compression');
const fs = require('fs-extra');
const MongoDBDatabase = require('./mongodb');

const collectionRoutes = require('./routes/collections');
const imageRoutes = require('./routes/images');
const cacheRoutes = require('./routes/cache');
const bulkRoutes = require('./routes/bulk');
const backgroundBulkRoutes = require('./routes/backgroundBulk');
const statsRoutes = require('./routes/stats');
const cacheFolderRoutes = require('./routes/cacheFolders');

const app = express();
const PORT = process.env.PORT || 8081;

// Initialize MongoDB
let db;
const initDatabase = async () => {
  try {
    db = new MongoDBDatabase();
    await db.connect();
    
    // Make database available to routes
    app.locals.db = db;
    
    console.log('📊 MongoDB connected successfully');
  } catch (error) {
    console.error('❌ MongoDB connection failed:', error);
    process.exit(1);
  }
};

// Middleware
app.use(compression());
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Ensure cache directories exist
const ensureDirectories = async () => {
  const cacheDir = path.join(__dirname, 'cache');
  const thumbnailsDir = path.join(cacheDir, 'thumbnails');
  const tempDir = path.join(__dirname, 'temp');
  
  await fs.ensureDir(cacheDir);
  await fs.ensureDir(thumbnailsDir);
  await fs.ensureDir(tempDir);
};

// API Routes (must come before static file serving)
app.use('/api/collections', collectionRoutes);
app.use('/api/images', imageRoutes);
app.use('/api/cache', cacheRoutes);
app.use('/api/bulk', bulkRoutes);
app.use('/api/background', backgroundBulkRoutes);
app.use('/api/stats', statsRoutes);
app.use('/api/cache-folders', cacheFolderRoutes);

// Health check
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// Serve static files
app.use('/static', express.static(path.join(__dirname, 'public')));

// Serve the built React app
app.use(express.static(path.join(__dirname, 'public')));

// Handle React routing, return all requests to React app
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// Error handling middleware
app.use((err, req, res, next) => {
  console.error('Error:', err);
  res.status(500).json({ error: 'Internal server error' });
});

// Start server
const startServer = async () => {
  try {
    await ensureDirectories();
    await initDatabase();
    
    app.listen(PORT, () => {
      console.log(`🚀 Server running on http://localhost:${PORT}`);
      console.log(`📁 Cache directory: ${path.join(__dirname, 'cache')}`);
    });
  } catch (error) {
    console.error('Failed to start server:', error);
    process.exit(1);
  }
};

startServer();
